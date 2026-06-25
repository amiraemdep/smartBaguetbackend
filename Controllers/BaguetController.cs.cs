using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SmartBaguet.API.Data;
using SmartBaguet.API.DTOs;
using SmartBaguet.API.Models;

namespace SmartBaguet.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class BaguetController : ControllerBase
{
    private readonly ApplicationDbContext _context;

    public BaguetController(ApplicationDbContext context)
    {
        _context = context;
    }
    [HttpPost("marriage")]
    public async Task<IActionResult> Marriage(MarriageDto dto)
    {
        // 🔥 Recherche baguet
        var baguet = await _context.Baguets
            .FirstOrDefaultAsync(x => x.CodeBaguet == dto.CodeBaguet);

        // 🔥 Création baguet si inexistant
        if (baguet == null)
        {
            baguet = new Baguet
            {
                CodeBaguet = dto.CodeBaguet,
                Status = "VIDE"
            };

            _context.Baguets.Add(baguet);

            await _context.SaveChangesAsync();
        }

        // 🔥 Vérification baguet
        if (baguet.Status != "VIDE")
        {
            return BadRequest(new
            {
                Message = "Baguet déjà utilisé"
            });
        }

        // 🔥 Recherche plant
        var plant = await _context.Plants
            .FirstOrDefaultAsync(x => x.CodePlant == dto.CodePlant);

        // 🔥 Si nouveau plant
        if (plant == null)
        {
            plant = new Plant
            {
                CodePlant = dto.CodePlant,
                Client = dto.Client
            };

            _context.Plants.Add(plant);

            await _context.SaveChangesAsync();
        }

        // 🔥 Mariage
        baguet.CurrentPlantId = plant.Id;
        baguet.Status = "CHARGE";

        // 🔥 Historique
        _context.BaguetHistories.Add(new BaguetHistory
        {
            CodeBaguet = dto.CodeBaguet,
            CodePlant = dto.CodePlant,
            DateEntree = DateTime.Now
        });

        await _context.SaveChangesAsync();

        return Ok(new
        {
            Message = "Mariage effectué avec succès",
            Client = plant.Client
        });
    }

    [HttpPost("vider-par-plant/{codePlant}")]
    public async Task<IActionResult> ViderParPlant(string codePlant)
    {
        // 🔥 Chercher baguet contenant ce plant
        var baguet = await _context.Baguets
            .Include(x => x.CurrentPlant)
            .FirstOrDefaultAsync(x => x.CurrentPlant!.CodePlant == codePlant);

        // 🔥 Vérification
        if (baguet == null)
        {
            return NotFound(new
            {
                Message = "Aucun baguet contient ce plant"
            });
        }

        // 🔥 Vider baguet
        baguet.Status = "VIDE";
        baguet.CurrentPlantId = null;

        // 🔥 Historique
        var history = await _context.BaguetHistories
            .Where(x => x.CodePlant == codePlant && x.DateSortie == null)
            .OrderByDescending(x => x.DateEntree)
            .FirstOrDefaultAsync();

        if (history != null)
        {
            history.DateSortie = DateTime.Now;
        }

        await _context.SaveChangesAsync();

        return Ok(new
        {
            Message = "Baguet vidé avec succès",
            CodeBaguet = baguet.CodeBaguet
        });
    }

    [HttpGet("history")]
    public async Task<IActionResult> GetHistory()
    {



        var history = await (
            from h in _context.BaguetHistories
            join p in _context.Plants
                on h.CodePlant equals p.CodePlant into plantGroup
            from p in plantGroup.DefaultIfEmpty()

            orderby h.DateEntree descending

            select new 
            {
                h.CodeBaguet,
                h.CodePlant,
                Client = p != null ? p.Client : null,
                h.DateEntree,
                h.DateSortie
            }
        )
        .Take(20)
        .ToListAsync();

        return Ok(history);
    }


    [HttpGet("plant/{codeBaguet}")]
    public async Task<IActionResult> GetByCodePlant(string codeBaguet)
    {
        var plant = await _context.Plants
            .FirstOrDefaultAsync(p => p.CodePlant == codeBaguet);

        if (plant == null)
        {
            return NotFound(new { message = $"Plant \"{codeBaguet}\" introuvable" });
        }

        var baguet = await _context.Baguets
            .FirstOrDefaultAsync(b => b.CurrentPlantId == plant.Id);

        var result = new
        {
            type = "success",
            codePlant = plant.CodePlant,
            codeBaguet = baguet?.CodeBaguet,
            client = plant.Client,
            ot = plant.OT,
            item = plant.ITEM,
            ordre = plant.Ordre,
            status = baguet?.Status ?? "NON_MARIE"
        };

        return Ok(result);
    }

    [HttpGet("stats")]
    public async Task<IActionResult> GetStats()
    {
        var totalBaguets = await _context.Baguets.CountAsync();

        var baguetsCharges = await _context.Baguets
            .CountAsync(x => x.Status == "CHARGE");

        var baguetsVides = await _context.Baguets
            .CountAsync(x => x.Status == "VIDE");

        var plantsAujourdhui = await _context.BaguetHistories
            .CountAsync(x => x.DateEntree.Date == DateTime.Today);

        return Ok(new
        {
            totalBaguets,
            baguetsCharges,
            baguetsVides,
            plantsAujourdhui
        });
    }
    [HttpGet("search/{code}")]
    public async Task<IActionResult> Search(string code)
    {
        var baguet = await _context.Baguets.FirstOrDefaultAsync(x => x.CodeBaguet == code);
        if (baguet != null) return await GetByCodeBaguet(code);
   

        if (baguet != null)
        {
            return await GetByCodeBaguet(code);
        }

        var plant = await _context.Plants
            .FirstOrDefaultAsync(x => x.CodePlant == code);

        if (plant != null)
        {
            return await GetByCodePlant(code);
        }

        return NotFound(new
        {
            message = $"Code {code} introuvable"
        });
    }
    private string GetTypeContrepartie(string type)
    {
        return type switch
        {
            "H" => "Hybrid",
            "C" => "Combiné",
            "P" => "Pneumatic",
            "A" => "Assembly",
            "V" => "Vision",
            "U" => "Pull",
            "W" => "Well Insertion",
            _ => "Inconnu"
        };
    }

    [HttpGet("by-baguet/{codeBaguet}")]
    public async Task<IActionResult> GetByCodeBaguet(string codeBaguet)
    {
        var baguet = await _context.Baguets
            .Include(b => b.CurrentPlant)
            .FirstOrDefaultAsync(b => b.CodeBaguet == codeBaguet);

        if (baguet == null)
        {
            return NotFound(new { message = $"Baguette {codeBaguet} introuvable" });
        }

        if (baguet.CurrentPlant == null)
        {
            return Ok(new
            {
                type = "warning",
                codeBaguet = baguet.CodeBaguet,
                status = "VIDE",
                message = "Cette baguette ne contient aucun plant"
            });
        }

        var plant = baguet.CurrentPlant;

        string annee = "";
        string ot = "";
        string item = "";
        string ordre = "";
        string typeCode = "";
        string typeContrepartie = "";

        // ⚠️ On décode depuis plant.CodePlant (14 caractères), pas codeBaguet (8 chiffres)
        if (!string.IsNullOrEmpty(plant.CodePlant) && plant.CodePlant.Length >= 14)
        {
            annee = "20" + plant.CodePlant.Substring(0, 2);
            ot = plant.CodePlant.Substring(2, 5);
            typeCode = plant.CodePlant.Substring(7, 1);
            item = plant.CodePlant.Substring(8, 3);
            ordre = plant.CodePlant.Substring(11, 3);
            typeContrepartie = GetTypeContrepartie(typeCode);
        }

        return Ok(new
        {
            type = "success",
            codePlant = plant.CodePlant,
            codeBaguet = baguet.CodeBaguet,
            client = plant.Client,
            annee,
            ot,
            item,
            ordre,
            typeCode,
            typeContrepartie,
            status = baguet.Status
        });
    }
}