namespace School.Web.Models
{
    public class PageViewModel
    {
        public int Id { get; set; }
        public int SrNo { get; set; }
        public string PageName { get; set; } = string.Empty;
        public string Url { get; set; } = string.Empty;
        public string RoutingPageUrl { get; set; } = string.Empty;
        public string Icon { get; set; } = string.Empty;
        public string MenuPage { get; set; } = string.Empty;
        public bool IsActive { get; set; }
        public int? ParentId { get; set; }
        public int DisplayOrder { get; set; }
    }

    public class PageTreeItemViewModel
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Icon { get; set; } = string.Empty;
        public int? ParentId { get; set; }
        public bool IsExpanded { get; set; }
        public List<PageTreeItemViewModel> Children { get; set; } = new();
    }

    public class AddPageViewModel
    {
        public PageViewModel Page { get; set; } = new();
        public List<PageTreeItemViewModel> TreeItems { get; set; } = new();
    }
}
