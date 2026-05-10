using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using TimeForPill.Data;
using TimeForPill.Models;

namespace TimeForPill
{
    public class TerapijasController : Controller
    {
        private readonly ApplicationDbContext _context;

        public TerapijasController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: Terapijas
        public async Task<IActionResult> Index()
        {
            return View(await _context.Terapije.ToListAsync());
        }

        // GET: Terapijas/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var terapija = await _context.Terapije
                .FirstOrDefaultAsync(m => m.Id == id);
            if (terapija == null)
            {
                return NotFound();
            }

            return View(terapija);
        }

        // GET: Terapijas/Create
        public IActionResult Create()
        {
            return View();
        }

        // POST: Terapijas/Create
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("Id,Status,Naziv,Pocetak,Kraj,DnevnaDoza,LijekId,PacijentId,NotifikacijaID")] Terapija terapija)
        {
            if (ModelState.IsValid)
            {
                _context.Add(terapija);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }
            return View(terapija);
        }

        // GET: Terapijas/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var terapija = await _context.Terapije.FindAsync(id);
            if (terapija == null)
            {
                return NotFound();
            }
            return View(terapija);
        }

        // POST: Terapijas/Edit/5
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("Id,Status,Naziv,Pocetak,Kraj,DnevnaDoza,LijekId,PacijentId,NotifikacijaID")] Terapija terapija)
        {
            if (id != terapija.Id)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(terapija);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!TerapijaExists(terapija.Id))
                    {
                        return NotFound();
                    }
                    else
                    {
                        throw;
                    }
                }
                return RedirectToAction(nameof(Index));
            }
            return View(terapija);
        }

        // GET: Terapijas/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var terapija = await _context.Terapije
                .FirstOrDefaultAsync(m => m.Id == id);
            if (terapija == null)
            {
                return NotFound();
            }

            return View(terapija);
        }

        // POST: Terapijas/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var terapija = await _context.Terapije.FindAsync(id);
            if (terapija != null)
            {
                _context.Terapije.Remove(terapija);
            }

            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        private bool TerapijaExists(int id)
        {
            return _context.Terapije.Any(e => e.Id == id);
        }
    }
}
