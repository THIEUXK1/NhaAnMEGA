using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace NhaAnMEGA.Models.Zktime;

[Table("KhachAnPF")]
[Index("MaThe", Name = "IX_KhachAnPfs_MaThe")]
public partial class KhachAnPf
{
    [Key]
    [Column("id")]
    public int Id { get; set; }

    [Column("Ma_the")]
    [StringLength(50)]
    public string? MaThe { get; set; }

    [Column("TEN_THE")]
    [StringLength(100)]
    public string? TenThe { get; set; }

    [Column("TEN_NT")]
    [StringLength(100)]
    public string? TenNt { get; set; }

    [Column("Nha_thau")]
    [StringLength(100)]
    public string? NhaThau { get; set; }

    [Column("Trang_thai")]
    [StringLength(50)]
    public string? TrangThai { get; set; }

    [Column("Tinh_trang")]
    [StringLength(50)]
    public string? TinhTrang { get; set; }
}
