using System.Text.Json;
using SignageApp.Models;

namespace SignageApp.Services;

public class SlideService
{
    private readonly string filePath;
    private List<Slide> slides;

    public SlideService()
    {
        filePath = ResolveStoragePath();
        slides = LoadSlides();
    }

    public List<Slide> GetSlides()
    {
        return slides
            .OrderBy(slide => slide.Volgorde)
            .Select(CloneSlide)
            .ToList();
    }

    public Slide? GetSlideById(int id)
    {
        Slide? slide = slides.FirstOrDefault(s => s.Id == id);
        return slide == null ? null : CloneSlide(slide);
    }

    public void AddSlide(Slide slide)
    {
        Slide slideToAdd = CloneSlide(slide);

        if (slides.Any(existingSlide => existingSlide.Id == slideToAdd.Id))
        {
            slideToAdd.Id = GetNextId();
        }

        slides.Add(slideToAdd);
        NormalizeOrder();
        SaveSlides();
    }

    public bool UpdateSlide(Slide updatedSlide)
    {
        int index = slides.FindIndex(slide => slide.Id == updatedSlide.Id);

        if (index == -1)
        {
            return false;
        }

        if (IsProtectedRssSlide(slides[index]))
        {
            return false;
        }

        slides[index] = CloneSlide(updatedSlide);
        NormalizeOrder();
        SaveSlides();
        return true;
    }

    public bool RemoveSlideById(int id)
    {
        Slide? slideToRemove = slides.FirstOrDefault(slide => slide.Id == id);

        if (slideToRemove == null)
        {
            return false;
        }

        if (IsProtectedRssSlide(slideToRemove))
        {
            return false;
        }

        slides.Remove(slideToRemove);
        NormalizeOrder();
        SaveSlides();
        return true;
    }

    public bool ReorderSlides(List<Slide> reorderedSlides)
    {
        if (reorderedSlides.Count != slides.Count)
        {
            return false;
        }

        List<int> currentIds = slides
            .Select(slide => slide.Id)
            .OrderBy(id => id)
            .ToList();

        List<int> reorderedIds = reorderedSlides
            .Select(slide => slide.Id)
            .OrderBy(id => id)
            .ToList();

        if (!currentIds.SequenceEqual(reorderedIds))
        {
            return false;
        }

        slides = reorderedSlides
            .Select(CloneSlide)
            .ToList();

        NormalizeOrder();
        SaveSlides();
        return true;
    }

    public int GetNextId()
    {
        if (slides.Count == 0)
        {
            return 1;
        }

        return slides.Max(slide => slide.Id) + 1;
    }

    private List<Slide> LoadSlides()
    {
        if (!File.Exists(filePath))
        {
            List<Slide> defaultSlides = CreateDefaultSlides();
            SaveSlides(defaultSlides);
            return defaultSlides;
        }

        try
        {
            string json = File.ReadAllText(filePath);

            if (string.IsNullOrWhiteSpace(json))
            {
                List<Slide> defaultSlides = CreateDefaultSlides();
                SaveSlides(defaultSlides);
                return defaultSlides;
            }

            List<Slide>? loadedSlides = JsonSerializer.Deserialize<List<Slide>>(json);

            if (loadedSlides == null || loadedSlides.Count == 0)
            {
                List<Slide> defaultSlides = CreateDefaultSlides();
                SaveSlides(defaultSlides);
                return defaultSlides;
            }

            return loadedSlides
                .OrderBy(slide => slide.Volgorde)
                .ToList();
        }
        catch
        {
            List<Slide> defaultSlides = CreateDefaultSlides();
            SaveSlides(defaultSlides);
            return defaultSlides;
        }
    }

    private void SaveSlides()
    {
        SaveSlides(slides);
    }

    private void SaveSlides(List<Slide> slidesToSave)
    {
        JsonSerializerOptions options = new JsonSerializerOptions
        {
            WriteIndented = true
        };

        string json = JsonSerializer.Serialize(slidesToSave.OrderBy(slide => slide.Volgorde), options);
        File.WriteAllText(filePath, json);
    }

    private void NormalizeOrder()
    {
        slides = slides
            .OrderBy(slide => slide.Volgorde)
            .ThenBy(slide => slide.Id)
            .ToList();

        for (int i = 0; i < slides.Count; i++)
        {
            slides[i].Volgorde = i + 1;
        }
    }

    private string ResolveStoragePath()
    {
        string currentDirectoryPath = Path.Combine(Directory.GetCurrentDirectory(), "slides.json");
        string runtimeDirectoryPath = Path.Combine(AppContext.BaseDirectory, "slides.json");

        if (File.Exists(currentDirectoryPath) || File.Exists(Path.Combine(Directory.GetCurrentDirectory(), "index.html")))
        {
            return currentDirectoryPath;
        }

        return runtimeDirectoryPath;
    }

    private bool IsProtectedRssSlide(Slide slide)
    {
        return slide.Type.Trim().ToLower() == "hypotheek"
               && slide.Bedrijf.Trim().ToLower() == "de financiële experts";
    }

    private Slide CloneSlide(Slide slide)
    {
        return new Slide
        {
            Id = slide.Id,
            Type = slide.Type,
            Bedrijf = slide.Bedrijf,
            Status = slide.Status,
            Titel = slide.Titel,
            Plaats = slide.Plaats,
            Prijs = slide.Prijs,
            EnergieLabel = slide.EnergieLabel,
            Afbeelding = slide.Afbeelding,
            Extra = slide.Extra,
            IsActief = slide.IsActief,
            Volgorde = slide.Volgorde
        };
    }

    private List<Slide> CreateDefaultSlides()
    {
        return new List<Slide>
        {
            new Slide
            {
                Id = 1,
                Type = "pand",
                Bedrijf = "Reliplan",
                Status = "Te koop",
                Titel = "Kerklaan 12",
                Plaats = "Rotterdam",
                Prijs = "€ 425.000 k.k.",
                EnergieLabel = "",
                Afbeelding = "https://placehold.co/1080x1350?text=Reliplan+Pand",
                Extra = "Mooie kantoorruimte op zichtlocatie",
                IsActief = true,
                Volgorde = 1
            },
            new Slide
            {
                Id = 2,
                Type = "pand",
                Bedrijf = "Vastgoed Experts",
                Status = "Verkocht",
                Titel = "Dorpsstraat 5",
                Plaats = "Nieuwerkerk aan den IJssel",
                Prijs = "€ 389.000 k.k.",
                EnergieLabel = "B",
                Afbeelding = "https://placehold.co/1080x1350?text=Vastgoed+Experts+Pand",
                Extra = "",
                IsActief = true,
                Volgorde = 2
            },
            new Slide
            {
                Id = 3,
                Type = "hypotheek",
                Bedrijf = "De Financiële Experts",
                Status = "",
                Titel = "",
                Plaats = "",
                Prijs = "",
                EnergieLabel = "",
                Afbeelding = "",
                Extra = "Actuele hypotheekrentes",
                IsActief = true,
                Volgorde = 3
            }
        };
    }
}