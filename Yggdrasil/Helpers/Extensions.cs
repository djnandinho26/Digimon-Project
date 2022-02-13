using System.Collections.Generic;

namespace Yggdrasil.Helpers;

public static class Extensions
{
    public static IEnumerable<T> DequeueChunk<T>(this Queue<T> queue, int chunkSize) 
    {
        for (int i = 0; i < chunkSize && queue.Count > 0; i++)
        {
            yield return queue.Dequeue();
        }
    }
}