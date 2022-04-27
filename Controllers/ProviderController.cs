using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using VirtualClinic.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Http;

namespace VirtualClinic.Controllers
{
    public class ProviderController : Controller
    {
        private MyContext dbContext;

        public ProviderController( MyContext context)
        {
            dbContext = context;
        }

        
        // PROVIDER DASHBOARD
        [HttpGet("providerdashboard")]
        public IActionResult ProviderDashboard()
        {
            if (HttpContext.Session.GetInt32("UserId") == null)
            {
                TempData["AuthError"] = "You must be logged in to view this page";
                return RedirectToAction("Index", "Home");
            }

            int UserId = (int) HttpContext.Session.GetInt32("UserId");

            ViewBag.userInDb = dbContext.Users.FirstOrDefault(p => p.UserId == UserId);

            return View();                      
        }

        // TEST PROVIDERDOCUMENATION

        [HttpGet("providerdocumentation")]
        public IActionResult ProviderDocumentation()
        {
            if (HttpContext.Session.GetInt32("UserId") == null)
            {
                TempData["AuthError"] = "You must be logged in to view this page";
                return RedirectToAction("Index", "Home");
            }
            
            var userInDb = dbContext.Users.FirstOrDefault(u => u.UserId == HttpContext.Session.GetInt32("UserId"));
            
            Console.WriteLine("User Logged In: ", HttpContext.Session.GetInt32("UserId"));
            
            ViewBag.UserLoggedIn = userInDb;

            // Patient Medications
            ViewBag.PatientInfo = dbContext.Patients
                                    .Include(p => p.Medications)
                                    .FirstOrDefault(p => p.UserId == userInDb.UserId);

            // Patient Allergies
            ViewBag.PatientAllergies = dbContext.Patients
                                        .Include(p => p.Allergies)
                                        .FirstOrDefault(p => p.UserId == userInDb.UserId);
    
            // Patient MedHx
            ViewBag.PatientMedHx = dbContext.Patients
                                    .Include(p => p.MedicalHistory)
                                    .FirstOrDefault(p => p.UserId == userInDb.UserId);

            // Patient Appointment
            ViewBag.PatientAppt = dbContext.Patients
                                    .Include(p => p.Appointments)
                                    .ThenInclude(l => l.Provider)
                                    .ThenInclude(u => u.User)
                                    .Include(p => p.Appointments)
                                    .ThenInclude(m => m.MedicalNotes)
                                    .FirstOrDefault(p => p.UserId == userInDb.UserId);

            // // List of Appointments Ordered by Date
            // ViewBag.AllAppointments = dbContext.Appointments
            //     .Include(p => p.Patient)
            //     .Include(p => p.Provider)
            //     .Where(p => p.PatientId == userInDb.UserId)
            //     .OrderBy(p => p.DateTime)
            //     .ToList();
            
            // Next appointment (not in the past)
            ViewBag.NextAppointment = dbContext.Appointments
                .Include(p => p.Patient)
                .Include(p => p.Provider)
                .ThenInclude(u => u.User)
                .Where(p => p.PatientId == userInDb.UserId && p.DateTime >= DateTime.Now.AddHours(-1))
                .OrderBy(p => p.DateTime)
                .FirstOrDefault();
            
            // Appointment with Id 1 and all includes
            ViewBag.Appointment1 = dbContext.Appointments
                .Include(p => p.Patient)
                .Include(p => p.Provider)
                .ThenInclude(u => u.User)
                .Include(p => p.MedicalNotes)
                .FirstOrDefault(p => p.AppointmentId == 1);
            
            
            

            ViewBag.MedicalNote1Exists = dbContext.MedicalNotes
                .Any(m => m.PatientId == 1 
                                     && m.ProviderId == 1
                                     && m.AppointmentId == 1);

            ViewBag.MedicalNote1 = dbContext.MedicalNotes
                .FirstOrDefault(m => m.PatientId == 1 
                          && m.ProviderId == 1
                          && m.AppointmentId == 1);

            
            return View();
        }

        // Post Medical Notes Model
        [HttpPost("add-medical-notes-autosave")]
        public IActionResult AddMedicalNotesAutoSave(MedicalNote newNote)
        {
            // Console.WriteLine("\n###############\n");
            // Console.WriteLine("New Medical Note: ");
            // Console.WriteLine(newNote.PatientId);
            // Console.WriteLine(newNote.AP);
            // // Console.WriteLine(;
            // // Console.WriteLine(HPI);
            // Console.WriteLine("\n###############\n");
            //     // dbContext.MedicalNotes.Add(newMedicalNote);
            //     // dbContext.SaveChanges();
                
            var newNoteInDb = dbContext.MedicalNotes
                .FirstOrDefault(m => m.PatientId == newNote.PatientId 
                                     && m.ProviderId == newNote.ProviderId
                                     && m.AppointmentId == newNote.AppointmentId);
            if (newNoteInDb != null)
            {
                newNoteInDb.HPI = newNote.HPI;
                newNoteInDb.PE = newNote.PE;
                newNoteInDb.Summary = newNote.Summary;
                newNoteInDb.AP = newNote.AP;
                
                // save changes
                dbContext.SaveChanges();
                
                var response = "Auto-updated Medical Notes";
            
                return Json(response);
            }
            else
            {
                dbContext.MedicalNotes.Add(newNote);
                dbContext.SaveChanges();
                
                var response = "Added new Medical Notes";
            
                return Json(response);
            }

        }
    }
}