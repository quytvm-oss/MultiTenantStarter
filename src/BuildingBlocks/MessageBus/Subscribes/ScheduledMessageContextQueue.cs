using System.Runtime.CompilerServices;
using MessageContext = MessageBus.Model.MessageContext;

namespace MessageBus.Subscribes;

/// <summary>
/// In-memory priority queue cho delayed messages.
/// 
/// Kết hợp với DB polling tạo thành hybrid 2-tầng:
///   Tầng 1 (DB poll ~5s): phát hiện message sắp đến hạn, nạp vào queue này
///   Tầng 2 (50ms loop):   đợi đúng sendTime rồi yield → Sender Task gửi broker
///
/// Độ chính xác: ±50ms (so với DB poll thuần: ±5s)
/// </summary>
// public sealed class ScheduledMessageContextQueue
// {
//     // SortedSet tự sort theo sendTime → First() luôn là message cần gửi sớm nhất
//     // Tie-break bằng DbId để tránh duplicate khi cùng sendTime
//     private readonly SortedSet<(long SendTime, MessageContext Message)> _queue
//         = new(Comparer<(long, MessageContext)>.Create((a, b) =>
//         {
//             int cmp = a.Item1.CompareTo(b.Item1);
//             return cmp != 0
//                 ? cmp
//                 : string.Compare(a.Item2.DbId, b.Item2.DbId, StringComparison.Ordinal);
//         }));
//
//     // Signal: mỗi Enqueue() release 1 token
//     // Consumer chỉ thức dậy khi thực sự có item mới
//     private readonly SemaphoreSlim _signal = new(0);
//     private readonly object        _lock   = new();
//
//     private const long TicksPerMs     = TimeSpan.TicksPerMillisecond;
//     private const long EarlyWindowTicks = 50 * TicksPerMs; // 50ms tolerance
//
//     // ── Public API ────────────────────────────────────────────────────
//
//     public void Enqueue(MessageContext message, DateTime sendTime)
//     {
//         lock (_lock)
//         {
//             _queue.Add((sendTime.Ticks, message));
//         }
//         _signal.Release(); // wake consumer
//     }
//
//     public int Count
//     {
//         get { lock (_lock) return _queue.Count; }
//     }
//
//     public IReadOnlyList<MessageContext> Snapshot()
//     {
//         lock (_lock)
//             return _queue.Select(x => x.Message).ToList();
//     }
//     
//     public IReadOnlyList<MessageContext> UnorderedItems
//     {
//         get
//         {
//             lock (_lock)
//             {
//                 return _queue.Select(x => x.Item2).ToList();
//             }
//         }
//     }
//
//     /// <summary>
//     /// Trả về message theo đúng thứ tự sendTime, đợi đến khi đến giờ mới yield.
//     /// Độ trễ tối đa: 50ms.
//     /// </summary>
//     public async IAsyncEnumerable<MessageContext> GetConsumingEnumerable(
//         [EnumeratorCancellation] CancellationToken cancellationToken = default)
//     {
//         while (!cancellationToken.IsCancellationRequested)
//         {
//             // Chờ signal từ Enqueue() — không busy-wait khi queue rỗng
//             await _signal.WaitAsync(cancellationToken).ConfigureAwait(false);
//
//             (long SendTime, MessageContext Message)? next = null;
//
//             lock (_lock)
//             {
//                 // Guard: semaphore có thể có token thừa (race giữa Release và Remove)
//                 if (_queue.Count == 0) continue;
//
//                 var top      = _queue.Min;
//                 var timeLeft = top.SendTime - DateTime.UtcNow.Ticks;
//
//                 if (timeLeft <= EarlyWindowTicks)
//                 {
//                     // Đến giờ (hoặc trong vòng 50ms) → dequeue và gửi
//                     _queue.Remove(top);
//                     next = top;
//                 }
//                 else
//                 {
//                     // Chưa đến giờ → trả token lại để không mất signal
//                     _signal.Release();
//                 }
//             }
//
//             if (next is not null)
//             {
//                 yield return next.Value.Message;
//             }
//             else
//             {
//                 // Ngủ ngắn rồi thử lại — tránh busy loop
//                 // Dùng min(timeLeft, 50ms) để không oversleep
//                 long remaining;
//                 lock (_lock)
//                 {
//                     remaining = _queue.Count > 0
//                         ? _queue.Min.SendTime - DateTime.UtcNow.Ticks
//                         : EarlyWindowTicks;
//                 }
//
//                 var sleepMs = Math.Clamp(remaining / TicksPerMs, 1, 50);
//                 await Task.Delay((int)sleepMs, cancellationToken).ConfigureAwait(false);
//             }
//         }
//     }
// }


public class ScheduledMessageContextQueue
{
    private readonly SortedSet<(long, MessageContext)> _queue = new(Comparer<(long, MessageContext)>.Create((a, b) =>
    {
        int result = a.Item1.CompareTo(b.Item1);
        return result == 0 ? String.Compare(a.Item2.DbId, b.Item2.DbId, StringComparison.Ordinal) : result;
    }));

    private readonly SemaphoreSlim _semaphore = new(0);
    private readonly object _lock = new();

    public void Enqueue(MessageContext message, long sendTime)
    {
        lock (_lock)
        {
            _queue.Add((sendTime, message));
        }
        
        _semaphore.Release();
    }
    
    public int Count
    {
        get
        {
            lock (_lock)
            {
                return _queue.Count;
            }
        }
    }
    
    public IEnumerable<MessageContext> UnorderedItems
    {
        get
        {
            lock (_lock)
            {
                return _queue.Select(x => x.Item2).ToList();
            }
        }
    }
    
    public async IAsyncEnumerable<MessageContext> GetConsumingEnumerable([EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        while (!cancellationToken.IsCancellationRequested)
        {
            await _semaphore.WaitAsync(cancellationToken);

            (long, MessageContext)? nextItem = null;

            lock (_lock)
            {
                if (_queue.Count > 0)
                {
                    var topMessage = _queue.First();
                    var timeLeft = topMessage.Item1 - DateTime.UtcNow.Ticks;
                    if (timeLeft < 500000) // 50ms
                    {
                        nextItem = topMessage;
                        _queue.Remove(topMessage);
                    }
                }
            }

            if (nextItem is not null)
            {
                yield return nextItem.Value.Item2;
            }
            else
            {
                // Re-release the semaphore if no item is ready yet
                _semaphore.Release();
                await Task.Delay(50, cancellationToken);
            }
        }
    }
}