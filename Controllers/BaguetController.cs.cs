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

}