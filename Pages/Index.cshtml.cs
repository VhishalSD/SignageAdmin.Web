using System.Globalization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using SignageApp.Models;
using SignageApp.Services;

namespace SignageAdmin.Web.Pages;

public class IndexModel : PageModel
{
    private readonly SlideService slideService;

    public List<Slide> Slides { get; private set; } = new();
    public List<Slide> SlidesForPreview { get; private set; } = new();

    [TempData]
    public string? SuccessMessage { get; set; }

    [BindProperty(SupportsGet = true)]
    public int? EditId { get; set; }

    [BindProperty]
    public string? Type { get; set; } = "pand";

    [BindProperty]
    public string? Bedrijf { get; set; }

    [BindProperty]
    public string? Status { get; set; }

    [BindProperty]
    public string? Titel { get; set; }

    [BindProperty]
    public string? Plaats { get; set; }

    [BindProperty]
    public string? Prijs { get; set; }

    [BindProperty]
    public string? PrijsSuffix { get; set; }

    [BindProperty]
    public string? EnergieLabel { get; set; }

    [BindProperty]
    public string? Afbeelding { get; set; }

    [BindProperty]
    public string? Extra { get; set; }

    [BindProperty]
    public bool IsActief { get; set; } = true;

    [BindProperty]
    public int DurationSeconds { get; set; } = 10;

    public bool IsEditMode => EditId.HasValue && EditId.Value > 0;

    public IndexModel(SlideService slideService)
    {
        this.slideService = slideService;
    }

    public void OnGet(int? editId)
    {
        Slides = slideService.GetSlides().OrderBy(slide => slide.Volgorde).ToList();
        SlidesForPreview = Slides.Where(slide => slide.IsActief).OrderBy(slide => slide.Volgorde).ToList();

        if (!editId.HasValue)
        {
            return;
        }

        Slide? slide = slideService.GetSlideById(editId.Value);

        if (slide == null)
        {
            return;
        }

        EditId = slide.Id;
        Type = slide.Type;
        Bedrijf = slide.Bedrijf;
        Status = slide.Status;
        Titel = slide.Titel;
        Plaats = slide.Plaats;
        Prijs = ExtractPriceAmount(slide.Prijs);
        PrijsSuffix = ExtractPriceSuffix(slide.Prijs);
        EnergieLabel = slide.EnergieLabel;
        Afbeelding = slide.Afbeelding;
        Extra = slide.Extra;
        IsActief = slide.IsActief;
        DurationSeconds = slide.DurationSeconds <= 0 ? 10 : slide.DurationSeconds;
    }

    public IActionResult OnPost()
    {
        if (!ValidateFormInput())
        {
            Slides = slideService.GetSlides().OrderBy(slide => slide.Volgorde).ToList();
            SlidesForPreview = Slides.Where(slide => slide.IsActief).OrderBy(slide => slide.Volgorde).ToList();
            return Page();
        }

        string finalType = string.IsNullOrWhiteSpace(Type) ? "pand" : Type;
        string finalBedrijf = finalType == "hypotheek"
            ? "De Financiële Experts"
            : (Bedrijf ?? "");

        if (IsEditMode)
        {
            Slide? existingSlide = slideService.GetSlideById(EditId!.Value);

            if (existingSlide == null)
            {
                return RedirectToPage();
            }

            Slide updatedSlide = new Slide
            {
                Id = existingSlide.Id,
                Type = finalType,
                Bedrijf = finalBedrijf,
                Status = finalType == "pand" ? (Status ?? "") : "",
                Titel = finalType == "pand" ? (Titel ?? "") : "",
                Plaats = finalType == "pand" ? (Plaats ?? "") : "",
                Prijs = finalType == "pand" ? BuildPrice(Prijs, PrijsSuffix) : "",
                EnergieLabel = finalBedrijf == "Vastgoed Experts" ? (EnergieLabel ?? "") : "",
                Afbeelding = finalType == "hypotheek" ? "" : (Afbeelding ?? ""),
                Extra = finalType == "pand" && finalBedrijf == "Reliplan" ? (Extra ?? "") : "",
                IsActief = IsActief,
                Volgorde = existingSlide.Volgorde,
                DurationSeconds = DurationSeconds
            };

            slideService.UpdateSlide(updatedSlide);
            SuccessMessage = "De slide is succesvol bijgewerkt.";
            return RedirectToPage();
        }

        Slide newSlide = new Slide
        {
            Id = slideService.GetNextId(),
            Type = finalType,
            Bedrijf = finalBedrijf,
            Status = finalType == "pand" ? (Status ?? "") : "",
            Titel = finalType == "pand" ? (Titel ?? "") : "",
            Plaats = finalType == "pand" ? (Plaats ?? "") : "",
            Prijs = finalType == "pand" ? BuildPrice(Prijs, PrijsSuffix) : "",
            EnergieLabel = finalBedrijf == "Vastgoed Experts" ? (EnergieLabel ?? "") : "",
            Afbeelding = finalType == "hypotheek" ? "" : (Afbeelding ?? ""),
            Extra = finalType == "pand" && finalBedrijf == "Reliplan" ? (Extra ?? "") : "",
            IsActief = IsActief,
            Volgorde = slideService.GetSlides().Count + 1,
            DurationSeconds = DurationSeconds
        };

        slideService.AddSlide(newSlide);
        SuccessMessage = "De slide is succesvol toegevoegd.";
        return RedirectToPage();
    }

    public IActionResult OnPostDelete(int id)
    {
        bool deleted = slideService.RemoveSlideById(id);

        if (deleted)
        {
            SuccessMessage = "De slide is succesvol verwijderd.";
        }

        return RedirectToPage();
    }

    public IActionResult OnPostActivate(int id)
    {
        Slide? slide = slideService.GetSlideById(id);

        if (slide != null)
        {
            slide.IsActief = true;
            slideService.UpdateSlide(slide);
            SuccessMessage = "De slide is geactiveerd.";
        }

        return RedirectToPage();
    }

    public IActionResult OnPostDeactivate(int id)
    {
        Slide? slide = slideService.GetSlideById(id);

        if (slide != null)
        {
            slide.IsActief = false;
            slideService.UpdateSlide(slide);
            SuccessMessage = "De slide is gedeactiveerd.";
        }

        return RedirectToPage();
    }

    public IActionResult OnPostMoveUp(int id)
    {
        List<Slide> slides = slideService.GetSlides()
            .OrderBy(slide => slide.Volgorde)
            .ToList();

        int index = slides.FindIndex(slide => slide.Id == id);

        if (index > 0)
        {
            Slide temp = slides[index - 1];
            slides[index - 1] = slides[index];
            slides[index] = temp;

            for (int i = 0; i < slides.Count; i++)
            {
                slides[i].Volgorde = i + 1;
            }

            slideService.ReorderSlides(slides);
            SuccessMessage = "De volgorde is aangepast.";
        }

        return RedirectToPage();
    }

    public IActionResult OnPostMoveDown(int id)
    {
        List<Slide> slides = slideService.GetSlides()
            .OrderBy(slide => slide.Volgorde)
            .ToList();

        int index = slides.FindIndex(slide => slide.Id == id);

        if (index >= 0 && index < slides.Count - 1)
        {
            Slide temp = slides[index + 1];
            slides[index + 1] = slides[index];
            slides[index] = temp;

            for (int i = 0; i < slides.Count; i++)
            {
                slides[i].Volgorde = i + 1;
            }

            slideService.ReorderSlides(slides);
            SuccessMessage = "De volgorde is aangepast.";
        }

        return RedirectToPage();
    }

    private bool ValidateFormInput()
    {
        if (string.IsNullOrWhiteSpace(Type))
        {
            ModelState.AddModelError(nameof(Type), "Kies een soort slide.");
        }

        if (Type == "pand")
        {
            if (string.IsNullOrWhiteSpace(Bedrijf))
            {
                ModelState.AddModelError(nameof(Bedrijf), "Kies een bedrijf.");
            }
            if (Bedrijf == "De Financiële Experts")
            {
                ModelState.AddModelError(nameof(Bedrijf), "De Financiële Experts is alleen bedoeld voor hypotheekslides.");
            }

            if (string.IsNullOrWhiteSpace(Status))
            {
                ModelState.AddModelError(nameof(Status), "Kies een status.");
            }

            if (string.IsNullOrWhiteSpace(Titel))
            {
                ModelState.AddModelError(nameof(Titel), "Titel of adres is verplicht.");
            }

            if (string.IsNullOrWhiteSpace(Plaats))
            {
                ModelState.AddModelError(nameof(Plaats), "Plaats is verplicht.");
            }

            if (string.IsNullOrWhiteSpace(Prijs))
            {
                ModelState.AddModelError(nameof(Prijs), "Bedrag is verplicht.");
            }
        }

        if (DurationSeconds < 1)
        {
            ModelState.AddModelError(nameof(DurationSeconds), "De duur moet minimaal 1 seconde zijn.");
        }

        if (!string.IsNullOrWhiteSpace(Afbeelding) && !IsValidImageInput(Afbeelding))
        {
            ModelState.AddModelError(nameof(Afbeelding), "Vul een geldige afbeeldingslink of bestandsnaam in.");
        }

        return ModelState.IsValid;
    }

    private bool IsValidImageInput(string? input)
    {
        string value = (input ?? "").Trim();

        if (string.IsNullOrWhiteSpace(value))
        {
            return true;
        }

        return value.StartsWith("http://", StringComparison.OrdinalIgnoreCase)
               || value.StartsWith("https://", StringComparison.OrdinalIgnoreCase)
               || value.Contains('.')
               || value.Contains('/');
    }

    private string BuildPrice(string? bedragInput, string? suffix)
    {
        string normalizedPrice = NormalizePrice(bedragInput);

        if (string.IsNullOrWhiteSpace(suffix))
        {
            return normalizedPrice;
        }

        return $"{normalizedPrice} {suffix}";
    }

    private string NormalizePrice(string? input)
    {
        string cleanedInput = (input ?? "").Trim();

        if (string.IsNullOrWhiteSpace(cleanedInput))
        {
            return "";
        }

        cleanedInput = cleanedInput.Replace("€", "").Trim();

        string numericPart = cleanedInput
            .Replace(" ", "")
            .Replace(".", "")
            .Replace(",", "");

        if (long.TryParse(numericPart, out long amount))
        {
            string formattedAmount = amount.ToString("N0", new CultureInfo("nl-NL"));
            return $"€ {formattedAmount}";
        }

        cleanedInput = cleanedInput.Replace(",", ".");
        return $"€ {cleanedInput}";
    }

    private string ExtractPriceAmount(string fullPrice)
    {
        string value = (fullPrice ?? "").Replace("€", "").Trim();

        if (value.EndsWith("k.k."))
        {
            value = value[..^4].Trim();
        }
        else if (value.EndsWith("v.o.n."))
        {
            value = value[..^6].Trim();
        }
        else if (value.EndsWith("p.m."))
        {
            value = value[..^4].Trim();
        }

        return value;
    }

    private string ExtractPriceSuffix(string fullPrice)
    {
        string value = (fullPrice ?? "").Trim().ToLower();

        if (value.EndsWith("k.k."))
        {
            return "k.k.";
        }

        if (value.EndsWith("v.o.n."))
        {
            return "v.o.n.";
        }

        if (value.EndsWith("p.m."))
        {
            return "p.m.";
        }

        return "";
    }
}