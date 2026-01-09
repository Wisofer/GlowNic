using Microsoft.EntityFrameworkCore;
using GlowNic.Models.Entities;

namespace GlowNic.Data;

public class ApplicationDbContext : DbContext
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
        : base(options)
    {
    }

    // Entidades del sistema nuevo (API)
    public DbSet<User> Users { get; set; }
    public DbSet<Barber> Barbers { get; set; }
    public DbSet<Employee> Employees { get; set; }
    
    // Entidades del sistema antiguo (MVC web - mantener compatibilidad)
    public DbSet<Usuario> Usuarios { get; set; }
    public DbSet<Service> Services { get; set; }
    public DbSet<Appointment> Appointments { get; set; }
    public DbSet<AppointmentServiceEntity> AppointmentServices { get; set; }
    public DbSet<WorkingHours> WorkingHours { get; set; }
    public DbSet<BlockedTime> BlockedTimes { get; set; }
    public DbSet<Transaction> Transactions { get; set; }
    public DbSet<Configuracion> Configuraciones { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Configuración de User
        modelBuilder.Entity<User>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Email).IsRequired().HasMaxLength(200);
            entity.Property(e => e.PasswordHash).IsRequired().HasMaxLength(500);
            entity.Property(e => e.Role).HasConversion<int>();
            entity.HasIndex(e => e.Email).IsUnique();
            
            entity.HasOne(e => e.Employee)
                .WithOne(emp => emp.User)
                .HasForeignKey<Employee>(e => e.UserId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        // Configuración de Barber
        modelBuilder.Entity<Barber>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Name).IsRequired().HasMaxLength(200);
            entity.Property(e => e.BusinessName).HasMaxLength(200);
            entity.Property(e => e.Phone).IsRequired().HasMaxLength(20);
            entity.Property(e => e.Slug).IsRequired().HasMaxLength(100);
            entity.HasIndex(e => e.Slug).IsUnique();
            entity.HasIndex(e => e.UserId).IsUnique();
            
            entity.HasOne(e => e.User)
                .WithOne(u => u.Barber)
                .HasForeignKey<Barber>(e => e.UserId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        // Configuración de Service
        modelBuilder.Entity<Service>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Name).IsRequired().HasMaxLength(200);
            entity.Property(e => e.Price).HasColumnType("decimal(18,2)");
            entity.Property(e => e.DurationMinutes).IsRequired();
            
            entity.HasOne(e => e.Barber)
                .WithMany(b => b.Services)
                .HasForeignKey(e => e.BarberId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        // Configuración de Employee
        modelBuilder.Entity<Employee>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Name).IsRequired().HasMaxLength(200);
            entity.Property(e => e.Phone).HasMaxLength(20);
            entity.HasIndex(e => e.UserId).IsUnique();
            
            entity.HasOne(e => e.OwnerBarber)
                .WithMany(b => b.Employees)
                .HasForeignKey(e => e.OwnerBarberId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        // Configuración de Appointment
        modelBuilder.Entity<Appointment>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.ClientName).IsRequired().HasMaxLength(200);
            entity.Property(e => e.ClientPhone).IsRequired().HasMaxLength(20);
            entity.Property(e => e.Status).HasConversion<int>();
            entity.Property(e => e.EmployeeId).IsRequired(false);
            
            entity.HasOne(e => e.Barber)
                .WithMany(b => b.Appointments)
                .HasForeignKey(e => e.BarberId)
                .OnDelete(DeleteBehavior.Cascade);
            
            entity.HasOne(e => e.Employee)
                .WithMany(emp => emp.Appointments)
                .HasForeignKey(e => e.EmployeeId)
                .OnDelete(DeleteBehavior.SetNull)
                .IsRequired(false);
            
            entity.HasOne(e => e.Service)
                .WithMany(s => s.Appointments)
                .HasForeignKey(e => e.ServiceId)
                .OnDelete(DeleteBehavior.SetNull)
                .IsRequired(false); // ServiceId es opcional
            
            entity.HasOne(e => e.Transaction)
                .WithOne(t => t.Appointment)
                .HasForeignKey<Transaction>(t => t.AppointmentId)
                .OnDelete(DeleteBehavior.SetNull);
            
            // Índice para evitar citas duplicadas
            entity.HasIndex(e => new { e.BarberId, e.Date, e.Time }).IsUnique();
        });

        // Configuración de WorkingHours
        modelBuilder.Entity<WorkingHours>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.DayOfWeek).HasConversion<int>();
            
            entity.HasOne(e => e.Barber)
                .WithMany(b => b.WorkingHours)
                .HasForeignKey(e => e.BarberId)
                .OnDelete(DeleteBehavior.Cascade);
            
            // Un salón solo puede tener un horario por día
            entity.HasIndex(e => new { e.BarberId, e.DayOfWeek }).IsUnique();
        });

        // Configuración de AppointmentServiceEntity (tabla intermedia)
        modelBuilder.Entity<AppointmentServiceEntity>(entity =>
        {
            entity.HasKey(e => e.Id);
            
            entity.HasOne(e => e.Appointment)
                .WithMany()
                .HasForeignKey(e => e.AppointmentId)
                .OnDelete(DeleteBehavior.Cascade);
            
            entity.HasOne(e => e.Service)
                .WithMany()
                .HasForeignKey(e => e.ServiceId)
                .OnDelete(DeleteBehavior.Cascade);
            
            // Evitar duplicados: un servicio no puede estar dos veces en la misma cita
            entity.HasIndex(e => new { e.AppointmentId, e.ServiceId }).IsUnique();
        });

        // Configuración de BlockedTime
        modelBuilder.Entity<BlockedTime>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Reason).HasMaxLength(500);
            
            entity.HasOne(e => e.Barber)
                .WithMany(b => b.BlockedTimes)
                .HasForeignKey(e => e.BarberId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        // Configuración de Transaction
        modelBuilder.Entity<Transaction>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Type).HasConversion<int>();
            entity.Property(e => e.Amount).HasColumnType("decimal(18,2)");
            entity.Property(e => e.Description).IsRequired().HasMaxLength(500);
            entity.Property(e => e.Category).HasMaxLength(100);
            entity.Property(e => e.EmployeeId).IsRequired(false);
            
            entity.HasOne(e => e.Barber)
                .WithMany(b => b.Transactions)
                .HasForeignKey(e => e.BarberId)
                .OnDelete(DeleteBehavior.Cascade);
            
            entity.HasOne(e => e.Employee)
                .WithMany(emp => emp.Transactions)
                .HasForeignKey(e => e.EmployeeId)
                .OnDelete(DeleteBehavior.SetNull)
                .IsRequired(false);
        });

        // Configuración de Configuracion (mantener existente)
        modelBuilder.Entity<Configuracion>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Clave).IsRequired().HasMaxLength(100);
            entity.Property(e => e.Valor).IsRequired().HasMaxLength(500);
            entity.Property(e => e.Descripcion).HasMaxLength(500);
            entity.Property(e => e.UsuarioActualizacion).HasMaxLength(200);
            entity.HasIndex(e => e.Clave).IsUnique();
        });
    }
}
