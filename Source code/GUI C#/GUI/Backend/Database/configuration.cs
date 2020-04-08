namespace GUI.Backend.Database
{
    using System;
    using System.Collections.Generic;
    using System.ComponentModel.DataAnnotations;
    using System.ComponentModel.DataAnnotations.Schema;
    using System.Data.Entity.Spatial;

    [Table("device_detection_db.configuration")]
    public partial class configuration
    {
        [Key]
        [Column(Order = 0)]
        [StringLength(100)]
        public string configuration_id { get; set; }

        [Key]
        [Column(Order = 1)]
        [StringLength(50)]
        public string board_id { get; set; }

        public double x { get; set; }

        public double y { get; set; }

        [Column(TypeName = "uint")]
        public long order { get; set; }

        [StringLength(255)]
        public string note { get; set; }
    }
}
