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


    [HttpGet("plant/{codePlant}")]
    public async Task<IActionResult> GetByCodePlant(string codePlant)
    {
        var plant = await _context.Plants
            .FirstOrDefaultAsync(p => p.CodePlant == codePlant);

        if (plant == null)
        {
            return NotFound(new { message = $"Plant \"{codePlant}\" introuvable" });
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

    [HttpGet("info/{codeBaguet}")]
    public async Task<IActionResult> GetByCodeBaguet(string codeBaguet)
    {
        var baguet = await _context.Baguets
            .FirstOrDefaultAsync(b => b.CodeBaguet == codeBaguet);

        if (baguet == null)
        {
            return NotFound(new { message = $"Baguet \"{codeBaguet}\" introuvable" });
        }

        if (baguet.Status == "VIDE" || baguet.CurrentPlantId == null)
        {
            return Ok(new
            {
                type = "vide",
                codeBaguet = baguet.CodeBaguet,
                status = baguet.Status,
                message = "Baguet vide, aucun plant à l'intérieur"
            });
        }

        var plant = await _context.Plants
            .FirstOrDefaultAsync(p => p.Id == baguet.CurrentPlantId);

        var result = new
        {
            type = "success",
            codeBaguet = baguet.CodeBaguet,
            codePlant = plant?.CodePlant,
            client = plant?.Client,
            ot = plant?.OT,
            item = plant?.ITEM, 
            ordre = plant?.Ordre,
            status = baguet.Status
        };

        return Ok(result);
    }
}