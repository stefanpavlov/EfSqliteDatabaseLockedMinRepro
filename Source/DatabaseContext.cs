using Microsoft.EntityFrameworkCore;

public sealed class ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : DbContext(options)
{
    public DbSet<File> Files { get; internal set; }
    public DbSet<Tape> Tapes { get; internal set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        //modelBuilder.Entity<File>()
        //    .HasMany(e => e.Tapes)
        //    .WithMany(e => e.Files)
        //    .UsingEntity<FileTape>(
        //        r =>
        //        {
        //            r.Property(ft => ft.TapeId).HasColumnName("TapesId");
        //            return r.HasOne<Tape>(ft => ft.Tape)
        //                .WithMany(t => t.FilesTapes)
        //                .HasForeignKey(ft => ft.TapeId);
        //        },
        //        l =>
        //        {
        //            l.Property(ft => ft.FileId).HasColumnName("FilesId");
        //            return l.HasOne<File>(ft => ft.File)
        //                .WithMany(f => f.FilesTapes)
        //                .HasForeignKey(ft => ft.FileId);
        //        });

        //modelBuilder.Entity<File>()
        //    .HasIndex(f => new { f.BucketId, f.Status });
    }
}
