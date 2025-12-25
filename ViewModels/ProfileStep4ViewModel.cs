using System.ComponentModel.DataAnnotations;

namespace DocAttestation.ViewModels;

public class ProfileStep4ViewModel
{
    public List<DocumentItem> Documents { get; set; } = new List<DocumentItem>();
    
    // Available document names for dropdown
    public static List<string> GetDocumentNames()
    {
        return new List<string>
        {
            // Educational Documents
            "Matriculation (SSC)",
            "Intermediate (HSSC)",
            "BA / BS (Graduation)",
            "MA / MSc (Master's)",
            "MPhil / PhD",
            "Diploma",
            "Technical Skills Certificate",
            "Experience Certificate",
            "Ongoing / In-Process Documents",
            "Others",
            
            // Personal / Official Documents
            "Birth Certificate",
            "Nikah Nama (Marriage Contract)",
            "Family Registration Certificate (FRC)",
            "Marriage Certificate",
            "Unmarried Certificate",
            "School Certificate",
            "Divorce Certificate",
            "Domicile / NOC",
            "Police Character Certificate",
            "Guardianship Certificate",
            
            // Other Documents
            "Medical Certificate",
            "Polio Card",
            "Death Certificate",
            "Bank Statement",
            "Affidavit",
            "Power of Attorney (Abroad)",
            "Power of Attorney (Within Pakistan)",
            "Passport (Additional Pages)",
            "Passport Copy",
            "Affidavit / Sworn Statement"
        };
    }
    
    // Get grouped document names for optgroups
    public static Dictionary<string, List<string>> GetGroupedDocumentNames()
    {
        return new Dictionary<string, List<string>>
        {
            {
                "Educational Documents",
                new List<string>
                {
                    "Matriculation (SSC)",
                    "Intermediate (HSSC)",
                    "BA / BS (Graduation)",
                    "MA / MSc (Master's)",
                    "MPhil / PhD",
                    "Diploma",
                    "Technical Skills Certificate",
                    "Experience Certificate",
                    "Ongoing / In-Process Documents",
                    "Others"
                }
            },
            {
                "Personal / Official Documents",
                new List<string>
                {
                    "Birth Certificate",
                    "Nikah Nama (Marriage Contract)",
                    "Family Registration Certificate (FRC)",
                    "Marriage Certificate",
                    "Unmarried Certificate",
                    "School Certificate",
                    "Divorce Certificate",
                    "Domicile / NOC",
                    "Police Character Certificate",
                    "Guardianship Certificate"
                }
            },
            {
                "Other Documents",
                new List<string>
                {
                    "Medical Certificate",
                    "Polio Card",
                    "Death Certificate",
                    "Bank Statement",
                    "Affidavit",
                    "Power of Attorney (Abroad)",
                    "Power of Attorney (Within Pakistan)",
                    "Passport (Additional Pages)",
                    "Passport Copy",
                    "Affidavit / Sworn Statement"
                }
            }
        };
    }
}

public class DocumentItem
{
    [Required(ErrorMessage = "Document name is required")]
    [Display(Name = "Document Name")]
    public string DocumentName { get; set; } = null!;
    
    [Required(ErrorMessage = "Document file is required")]
    [Display(Name = "Upload Document (PDF only)")]
    public IFormFile Document { get; set; } = null!;
}

