namespace GUI.Backend.Database
{
    using System;
    using System.Collections.Generic;
    using System.ComponentModel.DataAnnotations;
    using System.ComponentModel.DataAnnotations.Schema;
    using System.Data.Entity.Spatial;

    [Table("device_detection_db.log")]
    public partial class log
    {
        public int id { get; set; }

        public DateTime timestamp { get; set; }

        [StringLength(100)]
        public string configuration { get; set; }

        [Required]
        [StringLength(45)]
        public string type { get; set; }

        public int number_boards { get; set; }

        [Required]
        [StringLength(256)]
        public string message { get; set; }
    }
}
