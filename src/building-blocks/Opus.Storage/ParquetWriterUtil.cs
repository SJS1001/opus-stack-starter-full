using Parquet;
using Parquet.Serialization;

namespace Opus.Storage;

public static class ParquetWriterUtil
{
    public static async Task WriteAsync<T>(IEnumerable<T> items, Stream output) where T : class
    {
        await ParquetSerializer.SerializeAsync(items, output);
    }
}
