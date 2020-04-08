namespace GUI.Backend.Database
{
    using System;
    using System.Data.Entity;
    using System.ComponentModel.DataAnnotations.Schema;
    using System.Linq;

    public partial class DBmodel : DbContext
    {
        public DBmodel()
            : base("name=DBmodel")
        {
        }

        public virtual DbSet<configuration> configuration { get; set; }
        public virtual DbSet<log> log { get; set; }
        public virtual DbSet<position> position { get; set; }

        protected override void OnModelCreating(DbModelBuilder modelBuilder)
        {
            modelBuilder.Entity<configuration>()
                .Property(e => e.configuration_id)
                .IsUnicode(false);

            modelBuilder.Entity<configuration>()
                .Property(e => e.board_id)
                .IsUnicode(false);

            modelBuilder.Entity<configuration>()
                .Property(e => e.note)
                .IsUnicode(false);

            modelBuilder.Entity<log>()
                .Property(e => e.timestamp)
                .HasPrecision(3);

            modelBuilder.Entity<log>()
                .Property(e => e.configuration)
                .IsUnicode(false);

            modelBuilder.Entity<log>()
                .Property(e => e.type)
                .IsUnicode(false);

            modelBuilder.Entity<log>()
                .Property(e => e.message)
                .IsUnicode(false);

            modelBuilder.Entity<position>()
                .Property(e => e.timestamp)
                .HasPrecision(3);

            modelBuilder.Entity<position>()
                .Property(e => e.MACaddress)
                .IsUnicode(false);

            modelBuilder.Entity<position>()
                .Property(e => e.configuration_id)
                .IsUnicode(false);

            modelBuilder.Entity<position>()
                .Property(e => e.SSID)
                .IsUnicode(false);
        }
    }
}
