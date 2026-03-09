using LibCafe.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace LibCafe.Infrastructure.Data;
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
    public virtual DbSet<CafeTable> CafeTables { get; set; }

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        => optionsBuilder.UseNpgsql("Host=ep-still-surf-ahge1fih-pooler.c-3.us-east-1.aws.neon.tech;Database=neondb;Username=neondb_owner;Password=npg_I2Zonf1PYzCH;SSL Mode=Require;Channel Binding=Require");

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Book>(entity =>
        {
            entity.HasKey(e => e.Bookid).HasName("books_pkey");
            entity.ToTable("books");
            entity.HasIndex(e => e.Isbn, "books_isbn_key").IsUnique();
            entity.Property(e => e.Bookid).HasColumnName("bookid");
            entity.Property(e => e.Author).HasMaxLength(150).HasColumnName("author");
            entity.Property(e => e.Bookshelf).HasMaxLength(50).HasColumnName("bookshelf");
            entity.Property(e => e.Shelfnumber).HasMaxLength(50).HasColumnName("shelfnumber");
            entity.Property(e => e.Category).HasMaxLength(100).HasColumnName("category");
            entity.Property(e => e.Isbn).HasMaxLength(20).HasColumnName("isbn");
            entity.Property(e => e.Title).HasMaxLength(255).HasColumnName("title");
            entity.Property(e => e.Physicalcopies).HasColumnName("physicalcopies");
            entity.Property(e => e.Availablecopies).HasColumnName("availablecopies");
            entity.Property(e => e.Pdfurl).HasColumnName("pdfurl");
            entity.Property(e => e.Pdfavailable).HasColumnName("pdfavailable");
            entity.Property(e => e.Imagepath).HasColumnName("imagepath");
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
                .HasForeignKey(d => d.Bookid).HasConstraintName("bookreviews_bookid_fkey");
            entity.HasOne(d => d.User).WithMany(p => p.Bookreviews)
                .HasForeignKey(d => d.Userid).HasConstraintName("bookreviews_userid_fkey");
        });

        modelBuilder.Entity<Borrowing>(entity =>
        {
            entity.HasKey(e => e.Borrowingid).HasName("borrowed_books_pkey");
            entity.ToTable("borrowed_books");
            entity.Property(e => e.Borrowingid).HasColumnName("id");
            entity.Property(e => e.Userid).HasColumnName("user_id");
            entity.Property(e => e.Bookid).HasColumnName("book_id");
            entity.Property(e => e.BookTitle).HasColumnName("book_title");
            entity.Property(e => e.BookAuthor).HasColumnName("book_author");
            entity.Property(e => e.Borrowdate).HasColumnName("borrowed_at");
            entity.Property(e => e.Duedate).HasColumnName("due_date");
            entity.Property(e => e.Returndate).HasColumnName("returned_at");
            entity.Property(e => e.Status).HasColumnName("status");
            entity.HasOne(d => d.Book).WithMany(p => p.Borrowings)
                .HasForeignKey(d => d.Bookid).HasConstraintName("borrowed_books_book_id_fkey");
            entity.HasOne(d => d.User).WithMany(p => p.Borrowings)
                .HasForeignKey(d => d.Userid).HasConstraintName("borrowed_books_user_id_fkey");
        });

        modelBuilder.Entity<Cafeorder>(entity =>
        {
            entity.HasKey(e => e.Orderid).HasName("orders_pkey");
            entity.ToTable("orders");
            entity.Property(e => e.Orderid).HasColumnName("id");
            entity.Property(e => e.Userid).HasColumnName("user_id");
            entity.Property(e => e.Items).HasColumnName("items");
            entity.Property(e => e.Totalamount).HasColumnName("total_price");
            entity.Property(e => e.Status).HasColumnName("status");
            entity.Property(e => e.Orderdate).HasColumnName("order_time");
            entity.Property(e => e.Readytime).HasColumnName("ready_time");
            entity.Property(e => e.Completedat).HasColumnName("completed_time");
            entity.Property(e => e.Historytime).HasColumnName("history_time");
            entity.Property(e => e.Createdat).HasColumnName("created_at");
            entity.Property(e => e.Updatedat).HasColumnName("updated_at");
            entity.HasOne(d => d.User).WithMany(p => p.Cafeorders)
                .HasForeignKey(d => d.Userid).HasConstraintName("orders_user_id_fkey");
        });

        modelBuilder.Entity<Cafeorderitem>(entity =>
        {
            entity.HasKey(e => e.Orderitemid).HasName("cart_items_pkey");
            entity.ToTable("cart_items");
            entity.Property(e => e.Orderitemid).HasColumnName("id");
            entity.Property(e => e.Userid).HasColumnName("user_id");
            entity.Property(e => e.Itemid).HasColumnName("menu_item_id");
            entity.Property(e => e.Size).HasColumnName("size");
            entity.Property(e => e.Price).HasColumnName("price");
            entity.Property(e => e.Quantity).HasColumnName("quantity");
            entity.Property(e => e.Createdat).HasColumnName("created_at");
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
            entity.HasOne(d => d.User).WithMany(p => p.Cafereviews)
                .HasForeignKey(d => d.Userid).HasConstraintName("cafereviews_userid_fkey");
        });

        modelBuilder.Entity<Menuitem>(entity =>
        {
            entity.HasKey(e => e.Itemid).HasName("menu_items_pkey");
            entity.ToTable("menu_items");
            entity.Property(e => e.Itemid).HasColumnName("id");
            entity.Property(e => e.Itemname).HasMaxLength(150).HasColumnName("name");
            entity.Property(e => e.Description).HasColumnName("description");
            entity.Property(e => e.CategoryId).HasColumnName("category_id");
            entity.Property(e => e.Price).HasColumnName("price");
            entity.Property(e => e.Imagepath).HasColumnName("image");
            entity.Property(e => e.Available).HasColumnName("available");
            entity.Property(e => e.Rating).HasColumnName("rating");
        });

        modelBuilder.Entity<Payment>(entity =>
        {
            entity.HasKey(e => e.Paymentid).HasName("payments_pkey");
            entity.ToTable("payments");
            entity.Property(e => e.Paymentid).HasColumnName("paymentid");
            entity.Property(e => e.Amount).HasPrecision(10, 2).HasColumnName("amount");
            entity.Property(e => e.Orderid).HasColumnName("orderid");
            entity.Property(e => e.Paymentdate).HasColumnName("paymentdate");
            entity.Property(e => e.Paymentmethod).HasMaxLength(20).HasColumnName("paymentmethod");
            entity.Property(e => e.Userid).HasColumnName("userid");
            entity.HasOne(d => d.Order).WithMany(p => p.Payments)
                .HasForeignKey(d => d.Orderid).HasConstraintName("payments_orderid_fkey");
            entity.HasOne(d => d.User).WithMany(p => p.Payments)
                .HasForeignKey(d => d.Userid).HasConstraintName("payments_userid_fkey");
        });

        modelBuilder.Entity<Systemsetting>(entity =>
        {
            entity.HasKey(e => e.Settingid).HasName("systemsettings_pkey");
            entity.ToTable("systemsettings");
            entity.HasIndex(e => e.Settingname, "systemsettings_settingname_key").IsUnique();
            entity.Property(e => e.Settingid).HasColumnName("settingid");
            entity.Property(e => e.Settingname).HasMaxLength(100).HasColumnName("settingname");
            entity.Property(e => e.Settingvalue).HasMaxLength(255).HasColumnName("settingvalue");
        });

        modelBuilder.Entity<Notification>(entity =>
        {
            entity.HasKey(e => e.Notificationid).HasName("notifications_pkey");
            entity.ToTable("notifications");
            entity.Property(e => e.Notificationid).HasColumnName("notificationid");
            entity.Property(e => e.Userid).HasColumnName("userid");
            entity.Property(e => e.Title).HasMaxLength(200).HasColumnName("title");
            entity.Property(e => e.Message).HasColumnName("message");
            entity.Property(e => e.Type).HasMaxLength(50).HasColumnName("type");
            entity.Property(e => e.Isread).HasColumnName("isread");
            entity.Property(e => e.Relatedid).HasColumnName("relatedid");
            entity.Property(e => e.Createdat).HasColumnName("createdat");
            entity.HasOne(d => d.User).WithMany(p => p.Notifications)
                .HasForeignKey(d => d.Userid).HasConstraintName("notifications_userid_fkey");
        });

        modelBuilder.Entity<Reservation>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("reservations_pkey");
            entity.ToTable("reservations");
            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.TableId).HasColumnName("table_id");
            entity.Property(e => e.UserEmail).HasMaxLength(255).HasColumnName("user_email");
            entity.Property(e => e.UserName).HasMaxLength(150).HasColumnName("user_name");
            entity.Property(e => e.StartTime).HasColumnName("start_time");
            entity.Property(e => e.EndTime).HasColumnName("end_time");
            entity.Property(e => e.Status).HasMaxLength(20).HasColumnName("status");
            entity.Property(e => e.CreatedAt).HasColumnName("created_at");
            entity.HasOne(d => d.Table).WithMany()
           .HasForeignKey(d => d.TableId).HasConstraintName("reservations_table_id_fkey");
            entity.Ignore("Userid");
        });

        modelBuilder.Entity<CafeTable>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("tables_pkey");
            entity.ToTable("tables");
            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.TableNumber).HasMaxLength(10).HasColumnName("table_number");
            entity.Property(e => e.Capacity).HasColumnName("capacity");
            entity.Property(e => e.Type).HasMaxLength(20).HasColumnName("type");
            entity.Property(e => e.PositionRow).HasColumnName("position_row");
            entity.Property(e => e.PositionCol).HasColumnName("position_col");
            entity.Property(e => e.IsReserved).HasColumnName("is_reserved");
            entity.Property(e => e.CreatedAt).HasColumnName("created_at");
        });

        modelBuilder.Entity<User>(entity =>
        {
            entity.HasKey(e => e.Userid).HasName("users_pkey");
            entity.ToTable("users");
            entity.HasIndex(e => e.Email, "users_email_key").IsUnique();
            entity.Property(e => e.Userid).HasColumnName("id");
            entity.Property(e => e.Email).HasColumnName("email");
            entity.Property(e => e.Fullname).HasColumnName("name");
            entity.Property(e => e.Passwordhash).HasColumnName("password");
            entity.Property(e => e.Role).HasColumnName("role");
        });

        OnModelCreatingPartial(modelBuilder);
    }

    partial void OnModelCreatingPartial(ModelBuilder modelBuilder);
}