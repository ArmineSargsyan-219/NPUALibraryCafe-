using System;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore;

namespace NPUALibraryCafe.Models;

public partial class LibraryCafeDbContext : DbContext
{
    public LibraryCafeDbContext()
    {
    }

    public LibraryCafeDbContext(DbContextOptions<LibraryCafeDbContext> options)
        : base(options)
    {
    }

    public virtual DbSet<Book> Books { get; set; }

    public virtual DbSet<Bookreview> Bookreviews { get; set; }

    public virtual DbSet<Borrowing> Borrowings { get; set; }

    public virtual DbSet<Cafeorder> Cafeorders { get; set; }

    public virtual DbSet<Cafeorderitem> Cafeorderitems { get; set; }

    public virtual DbSet<Cafereview> Cafereviews { get; set; }

    public virtual DbSet<Menuitem> Menuitems { get; set; }

    public virtual DbSet<Payment> Payments { get; set; }

    public virtual DbSet<Systemsetting> Systemsettings { get; set; }

    public virtual DbSet<User> Users { get; set; }

    public virtual DbSet<Notification> Notifications { get; set; }

    public virtual DbSet<Reservation> Reservations { get; set; }

    public virtual DbSet<Reservationseat> Reservationseats { get; set; }


    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
#warning To protect potentially sensitive information in your connection string, you should move it out of source code. You can avoid scaffolding the connection string by using the Name= syntax to read it from configuration - see https://go.microsoft.com/fwlink/?linkid=2131148. For more guidance on storing connection strings, see https://go.microsoft.com/fwlink/?LinkId=723263.
        => optionsBuilder.UseNpgsql("Host=localhost;Port=5432;Database=library_cafe_system;Username=postgres;Password=596955");

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Book>(entity =>
        {
            entity.HasKey(e => e.Bookid).HasName("books_pkey");

            entity.ToTable("books");

            entity.HasIndex(e => e.Isbn, "books_isbn_key").IsUnique();

            entity.Property(e => e.Bookid).HasColumnName("bookid");
            entity.Property(e => e.Author)
                .HasMaxLength(150)
                .HasColumnName("author");
            entity.Property(e => e.Bookshelf)
                .HasMaxLength(50)
                .HasColumnName("bookshelf");
            entity.Property(e => e.Category)
                .HasMaxLength(100)
                .HasColumnName("category");
            entity.Property(e => e.Isbn)
                .HasMaxLength(20)
                .HasColumnName("isbn");
            entity.Property(e => e.Title)
                .HasMaxLength(255)
                .HasColumnName("title");
        });

        modelBuilder.Entity<Bookreview>(entity =>
        {
            entity.HasKey(e => e.Reviewid).HasName("bookreviews_pkey");

            entity.ToTable("bookreviews");

            entity.Property(e => e.Reviewid).HasColumnName("reviewid");
            entity.Property(e => e.Bookid).HasColumnName("bookid");
            entity.Property(e => e.Comment).HasColumnName("comment");
            entity.Property(e => e.Rating).HasColumnName("rating");
            entity.Property(e => e.Userid).HasColumnName("userid");

            entity.HasOne(d => d.Book).WithMany(p => p.Bookreviews)
                .HasForeignKey(d => d.Bookid)
                .HasConstraintName("bookreviews_bookid_fkey");

            entity.HasOne(d => d.User).WithMany(p => p.Bookreviews)
                .HasForeignKey(d => d.Userid)
                .HasConstraintName("bookreviews_userid_fkey");
        });

        modelBuilder.Entity<Borrowing>(entity =>
        {
            entity.HasKey(e => e.Borrowingid).HasName("borrowings_pkey");

            entity.ToTable("borrowings");

            entity.Property(e => e.Borrowingid).HasColumnName("borrowid");
            entity.Property(e => e.Bookid).HasColumnName("bookid");
            entity.Property(e => e.Borrowdate).HasColumnName("borrowdate");
            entity.Property(e => e.Duedate).HasColumnName("duedate");
            entity.Property(e => e.Returndate).HasColumnName("returndate");
            entity.Property(e => e.Userid).HasColumnName("userid");

            entity.HasOne(d => d.Book).WithMany(p => p.Borrowings)
                .HasForeignKey(d => d.Bookid)
                .HasConstraintName("borrowings_bookid_fkey");

            entity.HasOne(d => d.User).WithMany(p => p.Borrowings)
                .HasForeignKey(d => d.Userid)
                .HasConstraintName("borrowings_userid_fkey");
        });

        modelBuilder.Entity<Cafeorder>(entity =>
        {
            entity.HasKey(e => e.Orderid).HasName("cafeorders_pkey");

            entity.ToTable("cafeorders");

            entity.Property(e => e.Orderid).HasColumnName("orderid");
            entity.Property(e => e.Orderdate)
                .HasDefaultValueSql("CURRENT_TIMESTAMP")
                .HasColumnType("timestamp without time zone")
                .HasColumnName("orderdate");
            entity.Property(e => e.Ordertype)
                .HasMaxLength(20)
                .HasColumnName("ordertype");
            entity.Property(e => e.Status)
                .HasMaxLength(20)
                .HasDefaultValueSql("'Pending'::character varying")
                .HasColumnName("status");
            entity.Property(e => e.Totalamount)
                .HasPrecision(10, 2)
                .HasColumnName("totalamount");
            entity.Property(e => e.Userid).HasColumnName("userid");

            entity.HasOne(d => d.User).WithMany(p => p.Cafeorders)
                .HasForeignKey(d => d.Userid)
                .HasConstraintName("cafeorders_userid_fkey");
        });

        modelBuilder.Entity<Cafeorderitem>(entity =>
        {
            entity.HasKey(e => e.Orderitemid).HasName("cafeorderitems_pkey");

            entity.ToTable("cafeorderitems");

            entity.Property(e => e.Orderitemid).HasColumnName("orderitemid");
            entity.Property(e => e.Itemid).HasColumnName("itemid");
            entity.Property(e => e.Orderid).HasColumnName("orderid");
            entity.Property(e => e.Quantity).HasColumnName("quantity");

            entity.HasOne(d => d.Item).WithMany(p => p.Cafeorderitems)
                .HasForeignKey(d => d.Itemid)
                .HasConstraintName("cafeorderitems_itemid_fkey");

            entity.HasOne(d => d.Order).WithMany(p => p.Cafeorderitems)
                .HasForeignKey(d => d.Orderid)
                .HasConstraintName("cafeorderitems_orderid_fkey");
        });

        modelBuilder.Entity<Cafereview>(entity =>
        {
            entity.HasKey(e => e.Reviewid).HasName("cafereviews_pkey");

            entity.ToTable("cafereviews");

            entity.Property(e => e.Reviewid).HasColumnName("reviewid");
            entity.Property(e => e.Comment).HasColumnName("comment");
            entity.Property(e => e.Itemid).HasColumnName("itemid");
            entity.Property(e => e.Rating).HasColumnName("rating");
            entity.Property(e => e.Userid).HasColumnName("userid");

            entity.HasOne(d => d.Item).WithMany(p => p.Cafereviews)
                .HasForeignKey(d => d.Itemid)
                .HasConstraintName("cafereviews_itemid_fkey");

            entity.HasOne(d => d.User).WithMany(p => p.Cafereviews)
                .HasForeignKey(d => d.Userid)
                .HasConstraintName("cafereviews_userid_fkey");
        });

        modelBuilder.Entity<Menuitem>(entity =>
        {
            entity.HasKey(e => e.Itemid).HasName("menuitems_pkey");

            entity.ToTable("menuitems");

            entity.Property(e => e.Itemid).HasColumnName("itemid");
            entity.Property(e => e.Category)
                .HasMaxLength(50)
                .HasColumnName("category");
            entity.Property(e => e.Itemname)
                .HasMaxLength(150)
                .HasColumnName("itemname");
            entity.Property(e => e.Price)
                .HasPrecision(10, 2)
                .HasColumnName("price");
        });

        modelBuilder.Entity<Payment>(entity =>
        {
            entity.HasKey(e => e.Paymentid).HasName("payments_pkey");

            entity.ToTable("payments");

            entity.Property(e => e.Paymentid).HasColumnName("paymentid");
            entity.Property(e => e.Amount)
                .HasPrecision(10, 2)
                .HasColumnName("amount");
            entity.Property(e => e.Orderid).HasColumnName("orderid");
            entity.Property(e => e.Paymentdate)
                .HasDefaultValueSql("CURRENT_TIMESTAMP")
                .HasColumnType("timestamp without time zone")
                .HasColumnName("paymentdate");
            entity.Property(e => e.Paymentmethod)
                .HasMaxLength(20)
                .HasColumnName("paymentmethod");
            entity.Property(e => e.Userid).HasColumnName("userid");

            entity.HasOne(d => d.Order).WithMany(p => p.Payments)
                .HasForeignKey(d => d.Orderid)
                .HasConstraintName("payments_orderid_fkey");

            entity.HasOne(d => d.User).WithMany(p => p.Payments)
                .HasForeignKey(d => d.Userid)
                .HasConstraintName("payments_userid_fkey");
        });

        modelBuilder.Entity<Systemsetting>(entity =>
        {
            entity.HasKey(e => e.Settingid).HasName("systemsettings_pkey");

            entity.ToTable("systemsettings");

            entity.HasIndex(e => e.Settingname, "systemsettings_settingname_key").IsUnique();

            entity.Property(e => e.Settingid).HasColumnName("settingid");
            entity.Property(e => e.Settingname)
                .HasMaxLength(100)
                .HasColumnName("settingname");
            entity.Property(e => e.Settingvalue)
                .HasMaxLength(255)
                .HasColumnName("settingvalue");
        });

        modelBuilder.Entity<User>(entity =>
        {
            entity.HasKey(e => e.Userid).HasName("users_pkey");

            entity.ToTable("users");

            entity.HasIndex(e => e.Email, "users_email_key").IsUnique();

            entity.Property(e => e.Userid).HasColumnName("userid");
            entity.Property(e => e.Email)
                .HasMaxLength(150)
                .HasColumnName("email");
            entity.Property(e => e.Fullname)
                .HasMaxLength(100)
                .HasColumnName("fullname");
            entity.Property(e => e.Passwordhash)
                .HasMaxLength(255)
                .HasColumnName("passwordhash");
            entity.Property(e => e.Role)
                .HasMaxLength(20)
                .HasColumnName("role");
        });

        OnModelCreatingPartial(modelBuilder);
    }

    partial void OnModelCreatingPartial(ModelBuilder modelBuilder);
}
