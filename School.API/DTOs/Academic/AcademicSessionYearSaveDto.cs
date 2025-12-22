namespace School.API.DTOs.Academic
{
    public class AcademicSessionYearSaveDto
    {
        public int VID { get; set; } = 0;
        public string VName { get; set; }
        public DateTime DateFrom { get; set; }
        public DateTime DateTo { get; set; }
        public bool IsActive { get; set; } = true;
    }
}
