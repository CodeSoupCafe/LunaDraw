## UUID/GUID Performance in PostgreSQL with C#

The primary performance challenge with UUIDs as primary keys stems from **index fragmentation** caused by their random nature. This impacts both write (INSERT) and read performance (JOINs, lookups).

### Comparison Table

| Feature         | Random UUID (v4 / C# `Guid.NewGuid()`)      | Time-Based UUID (v7 / Sequential)         | Auto-incrementing Integer (`int` / `bigint`) |
| --------------- | ------------------------------------------- | ----------------------------------------- | -------------------------------------------- |
| **Indexing**    | High fragmentation, random disk I/O         | Low fragmentation, sequential disk I/O    | Very efficient, sequential data storage      |
| **Write Speed** | Slower due to page splits in B-tree indexes | Significantly faster inserts than v4      | Fastest write performance                    |
| **Read Speed**  | Slower lookups/range scans                  | Faster lookups/range scans than v4        | Fastest read performance for joins/lookups   |
| **Storage**     | 16 bytes (stored as `uuid` type)            | 16 bytes (stored as `uuid` type)          | 4 bytes (`int`) or 8 bytes (`bigint`)        |
| **Generation**  | `Guid.NewGuid()` in C# is simple            | C# .NET 9+ offers `Guid.CreateVersion7()` | Handled by database (requires round trip)    |

---

### Key Performance Considerations

- **Randomness Kills Performance:** Standard C# `Guid.NewGuid()` generates **Version 4 UUIDs**, which are highly random. When used as a primary key, this causes B-tree indexes to fragment, leading to slower writes and reads as the database constantly splits index pages to make room for new, non-sequential values.
- **Sequential Solves Fragmentation:** Use **Version 7 UUIDs**, which incorporate a time component at the beginning. This makes them largely sequential, significantly reducing index fragmentation and improving insertion performance. They are the modern standard for distributed systems needing high performance.
- **Storage Matters:** Always store UUIDs using PostgreSQL's native `uuid` data type (16 bytes). **Avoid** `CHAR(36)` or `VARCHAR` strings, which drastically increase storage size and slow down comparison operations.
- **The Best of Both Worlds:** A common architectural pattern uses an `integer` or `bigint` as the **internal primary key** (for optimal database performance in joins) and a UUID as a separate, **application-facing identifier** (for security, URL obscurity, and API exposure).

Would you like me to show you how to implement a UUID v7 generator for a version of .NET older than .NET 9?
