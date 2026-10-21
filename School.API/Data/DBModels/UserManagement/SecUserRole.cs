using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace School.API.Data.DBModels.UserManagement
{
    /// <summary>
    /// Which role a login has. One row per user per role.
    /// </summary>
    /// <remarks>
    /// Separate from UserRoles on purpose. That table's foreign key points at
    /// Users, the Entity-Framework user table; the application logs in against
    /// SecUser, which is also what the Users form creates and deletes. This
    /// table keys to SecUser so the roles picked on that form attach to the
    /// login they were picked for.
    /// </remarks>
    [Table("SecUserRole")]
    public class SecUserRole
    {
        [Key]
        public int Id { get; set; }

        /// <summary>SecUser.VID.</summary>
        public int UserVID { get; set; }

        /// <summary>Roles.Id.</summary>
        public int RoleId { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.Now;
    }
}
