# .NET 11 request compression

Run from `src/`: `dotnet run --project examples/Documentation/Bodies/Compression/Compression.csproj -c Release`.
The generated client checks gzip, Brotli and Zstandard per-coding options, exact Content-Encoding headers,
and decompressed JSON using generated System.Text.Json metadata. It also checks level-based Zstandard
when RequestCompressionOptions is null. Zstandard is intentionally isolated from the net10 documentation runner.

For native verification: `dotnet publish examples/Documentation/Bodies/Compression/Compression.csproj -c Release -r linux-x64`.
The project targets net11 and keeps C#14 syntax, trim analysis and AOT analysis enabled.
