using IteratorPattern;

// ============================================================
// ITERATOR PATTERN — DEMO
// ============================================================
// A music playlist iterated three ways: sequential, shuffle,
// and filtered. Each iterator encapsulates its own traversal
// logic — the playlist doesn't change, only the iterator does.
// ============================================================

static void Pause()
{
    Console.WriteLine();
    Console.Write("Press any key to continue...");
    Console.ReadKey(intercept: true);
    Console.WriteLine("\n");
}

static void PlayAll(IPlaylistIterator iterator)
{
    int track = 1;
    while (iterator.HasNext)
    {
        var song = iterator.Next();
        Console.WriteLine($"  {track++,2}. {song.Title,-30} {song.Artist,-20} [{song.Genre,-8}]  {song.DurationSeconds / 60}:{song.DurationSeconds % 60:D2}");
    }
}

// ── Seed playlist ─────────────────────────────────────────────
var playlist = new Playlist("My Mix");
playlist.Add(new Song("Bohemian Rhapsody",   "Queen",           "Rock",       354));
playlist.Add(new Song("Hotel California",    "Eagles",          "Rock",       391));
playlist.Add(new Song("Blinding Lights",     "The Weeknd",      "Pop",        200));
playlist.Add(new Song("Shape of You",        "Ed Sheeran",      "Pop",        234));
playlist.Add(new Song("Lose Yourself",       "Eminem",          "Hip-Hop",    326));
playlist.Add(new Song("HUMBLE.",             "Kendrick Lamar",  "Hip-Hop",    177));
playlist.Add(new Song("Stairway to Heaven",  "Led Zeppelin",    "Rock",       482));
playlist.Add(new Song("Rolling in the Deep", "Adele",           "Pop",        228));

Console.WriteLine("╔══════════════════════════════════════════════════════╗");
Console.WriteLine("║       ITERATOR PATTERN — Music Playlist              ║");
Console.WriteLine("╚══════════════════════════════════════════════════════╝");
Console.WriteLine();
Console.WriteLine($"Playlist: \"{playlist.Name}\"  ({playlist.Count} songs)");
Console.WriteLine();
Console.WriteLine("Three iterators, same playlist:");
Console.WriteLine("  SequentialIterator — songs in original order");
Console.WriteLine("  ShuffleIterator    — songs in randomised order");
Console.WriteLine("  FilterIterator     — only songs matching a predicate");
Console.WriteLine();
Console.WriteLine("The playlist never changes — only the traversal strategy does.");

Pause();

// ── DEMO 1: Sequential ────────────────────────────────────────
Console.WriteLine("━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━");
Console.WriteLine("DEMO 1 — SequentialIterator: original order");
Console.WriteLine("━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━");
Console.WriteLine();

PlayAll(playlist.GetSequentialIterator());

Pause();

// ── DEMO 2: Shuffle ───────────────────────────────────────────
Console.WriteLine("━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━");
Console.WriteLine("DEMO 2 — ShuffleIterator: randomised order (seed=42)");
Console.WriteLine("━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━");
Console.WriteLine();

PlayAll(playlist.GetShuffleIterator(seed: 42));

Pause();

// ── DEMO 3: Filter ────────────────────────────────────────────
Console.WriteLine("━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━");
Console.WriteLine("DEMO 3 — FilterIterator: Rock songs only");
Console.WriteLine("━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━");
Console.WriteLine();

PlayAll(playlist.GetFilterIterator(s => s.Genre == "Rock"));

Console.WriteLine();
Console.WriteLine("FilterIterator: songs longer than 5 minutes:");
Console.WriteLine();
PlayAll(playlist.GetFilterIterator(s => s.DurationSeconds > 300));

Pause();

// ── DEMO 4: Reset ─────────────────────────────────────────────
Console.WriteLine("━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━");
Console.WriteLine("DEMO 4 — Reset: play first two songs, reset, play again");
Console.WriteLine("━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━");
Console.WriteLine();

var iter = playlist.GetSequentialIterator();

Console.WriteLine("Playing first two songs:");
Console.WriteLine($"  1. {iter.Next().Title}");
Console.WriteLine($"  2. {iter.Next().Title}");
Console.WriteLine($"  HasNext = {iter.HasNext}");

iter.Reset();
Console.WriteLine();
Console.WriteLine("After Reset() — back to the beginning:");
Console.WriteLine($"  1. {iter.Next().Title}");
Console.WriteLine($"  2. {iter.Next().Title}");

Pause();

// ── DEMO 5: yield return — the C# native Iterator ─────────────
Console.WriteLine("━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━");
Console.WriteLine("DEMO 5 — yield return: C#'s built-in Iterator pattern");
Console.WriteLine("━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━");
Console.WriteLine();
Console.WriteLine("Playlist.AsEnumerable() uses yield return internally.");
Console.WriteLine("The compiler generates the same HasNext/Next state machine.");
Console.WriteLine("foreach consumes it via IEnumerable<Song>:");
Console.WriteLine();

int n = 1;
foreach (var song in playlist.AsEnumerable())
    Console.WriteLine($"  {n++,2}. {song.Title}");

Console.WriteLine();
Console.WriteLine("━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━");
Console.WriteLine("Summary:");
Console.WriteLine("  • IPlaylistIterator   — HasNext / Next / Reset");
Console.WriteLine("  • SequentialIterator  — index walks forward through the list");
Console.WriteLine("  • ShuffleIterator     — Fisher-Yates shuffle on construction");
Console.WriteLine("  • FilterIterator      — pre-filters to matching songs");
Console.WriteLine("  • yield return        — C# compiler generates IEnumerator state machine");
Console.WriteLine("  • Playlist unchanged  — only the traversal strategy varies");
Console.WriteLine("━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━");
