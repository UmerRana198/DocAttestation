using System.ComponentModel.DataAnnotations;
using DocAttestation.Models;

namespace DocAttestation.ViewModels;

public class ProfileStep4ViewModel
{
    public List<DocumentItem> Documents { get; set; } = new List<DocumentItem>();

    // Document Submission Method
    public DocumentSubmissionMethod? DocumentSubmissionMethod { get; set; }
    public SubmissionBy? SubmissionBy { get; set; }
    public string? RelationType { get; set; }
    public string? RelationCNIC { get; set; }
    
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
            "Transcript (Intermediate)",
            "Transcript (BA / BS)",
            "Transcript (MA / MSc)",
            "Transcript (MPhil / PhD)",
            "Transcript (Diploma)",
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
            "Divorce",
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
            "Power of Attorney (From)",
            "Power of Attorney (To)",
            "Passport (Additional Pages)",
            "Passport Copy",
            "Affidavit / Sworn Statement",

            // Legal Documents
            "Legal Documents",

            // Commercial Documents
            "Commercial Documents"
        };
    }
    
    // Get grouped document names with sub-categories for optgroups
    public static Dictionary<string, List<string>> GetGroupedDocumentNames()
    {
        return new Dictionary<string, List<string>>
        {
            {
                "📚 Educational Documents - School Level",
                new List<string>
                {
                    "Matriculation (SSC)",
                    "School Certificate"
                }
            },
            {
                "🏫 School Level",
                new List<string>
                {
                    "Matriculation (SSC)",
                    "School Certificate"
                }
            },
            {
                "🎓 Degrees",
                new List<string>
                {
                    "Intermediate (HSSC)",
                    "BA / BS (Graduation)",
                    "MA / MSc (Master's)",
                    "MPhil / PhD",
                    "Diploma"
                }
            },
            {
                "📄 Transcripts",
                new List<string>
                {
                    "Transcript (Intermediate)",
                    "Transcript (BA / BS)",
                    "Transcript (MA / MSc)",
                    "Transcript (MPhil / PhD)",
                    "Transcript (Diploma)"
                }
            },
            {
                "📜 Professional Certificates",
                new List<string>
                {
                    "Technical Skills Certificate",
                    "Experience Certificate",
                    "Ongoing / In-Process Documents",
                    "Others"
                }
            },
            {
                "👤 Personal Documents - Identity & Birth",
                new List<string>
                {
                    "Birth Certificate",
                    "Family Registration Certificate (FRC)",
                    "Domicile / NOC"
                }
            },
            {
                "👤 Personal Documents - Marriage Related",
                new List<string>
                {
                    "Nikah Nama (Marriage Contract)",
                    "Marriage Certificate",
                    "Unmarried Certificate",
                    "Divorce Certificate",
                    "Divorce"
                }
            },
            {
                "👤 Personal Documents - Legal & Character",
                new List<string>
                {
                    "Police Character Certificate",
                    "Guardianship Certificate"
                }
            },
            {
                "🏥 Other Documents - Medical & Health",
                new List<string>
                {
                    "Medical Certificate",
                    "Polio Card",
                    "Death Certificate"
                }
            },
            {
                "💰 Other Documents - Financial",
                new List<string>
                {
                    "Bank Statement"
                }
            },
            {
                "⚖️ Other Documents - Legal Documents",
                new List<string>
                {
                    "Affidavit",
                    "Affidavit / Sworn Statement",
                    "Legal Documents"
                }
            },
            {
                "📝 Other Documents - Power of Attorney",
                new List<string>
                {
                    "Power of Attorney (Abroad)",
                    "Power of Attorney (Within Pakistan)",
                    "Power of Attorney (From)",
                    "Power of Attorney (To)"
                }
            },
            {
                "✈️ Other Documents - Travel & Identity",
                new List<string>
                {
                    "Passport (Additional Pages)",
                    "Passport Copy"
                }
            },
            {
                "🏢 Commercial Documents",
                new List<string>
                {
                    "Commercial Documents"
                }
            }
        };
    }

    // Documents that require physical appearance only
    public static List<string> GetPhysicalOnlyDocuments()
    {
        return new List<string>
        {
            "Divorce",
            "Divorce Certificate",
            "Power of Attorney (From)",
            "Power of Attorney (To)",
            "Legal Documents",
            "Affidavit",
            "Commercial Documents"
        };
    }

    // Check if any selected documents require physical submission only
    public bool HasPhysicalOnlyDocuments()
    {
        if (Documents == null || Documents.Count == 0)
            return false;

        var physicalOnlyDocs = GetPhysicalOnlyDocuments();
        return Documents.Any(d => !string.IsNullOrEmpty(d.DocumentName) &&
                                 physicalOnlyDocs.Contains(d.DocumentName));
    }
}

public class DocumentItem
{
    [Required(ErrorMessage = "Document name is required")]
    [Display(Name = "Document Name")]
    public string DocumentName { get; set; } = string.Empty;

    [Required(ErrorMessage = "Front side document is required")]
    [Display(Name = "Front Side Document")]
    public IFormFile? FrontDocument { get; set; }

    [Required(ErrorMessage = "Back side document is required")]
    [Display(Name = "Back Side Document")]
    public IFormFile? BackDocument { get; set; }
}

