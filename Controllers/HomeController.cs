﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using qwerty.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Http;

namespace qwerty.Controllers
{
    public class HomeController : Controller
    {
        private MyContext dbContext;
        public HomeController (MyContext context)
        {
            dbContext = context;
        }
        [HttpGet("")]
        public IActionResult Index()
        {
            return View();
        }

        [HttpGet("login")]
        public IActionResult deadLogin()
        {
            return RedirectToAction("Index");
        }
        [HttpGet("register")]
        public IActionResult deadRegistration()
        {
            return RedirectToAction("Index");
        }

        [HttpPost("register")]
        public IActionResult Register(IndexViewModel modelData)
        {
            if(modelData == null)
            {
                return View("Index");
            }

            User submittedUser = modelData.newUser;
            if(ModelState.IsValid)
            {
                if(dbContext.Users.Any(u => u.Email == submittedUser.Email))
                {
                    ModelState.AddModelError("newUser.Email", "Email is already in use");
                    return View("Index");
                }

                dbContext.Users.Add(submittedUser);
                dbContext.SaveChanges();

                PasswordHasher<User> Hasher = new PasswordHasher<User>();
                submittedUser.Password = Hasher.HashPassword(submittedUser, submittedUser.Password);
                dbContext.SaveChanges();

                User current_user = dbContext.Users.FirstOrDefault(u => u.Email == submittedUser.Email);
                HttpContext.Session.SetInt32("Current_User_Id", current_user.UserId);
                int user_id = current_user.UserId;
                return Redirect($"home");
            }
            return View("Index");
        }

        [HttpPost("login")]
        public IActionResult Login(IndexViewModel modelData)
        {
            if(modelData == null)
            {
                return View("Index");
            }
            LoginUser submittedUser = modelData.loginUser;
            if(ModelState.IsValid)
            {
                var userInDb = dbContext.Users.FirstOrDefault(u=> u.Email == submittedUser.Email);
                if (userInDb == null)
                {
                    ModelState.AddModelError("loginUser.Email", "Invalid Email/Password");
                    return View("Index");
                }
                PasswordHasher<LoginUser> hasher = new PasswordHasher<LoginUser>();
                var result = hasher.VerifyHashedPassword(submittedUser, userInDb.Password, submittedUser.Password);
                if(result == 0)
                {
                    ModelState.AddModelError("loginUser.Password", "Invalid Email/Password");
                    return View("Index");
                }
                User current_user = dbContext.Users.FirstOrDefault(u => u.Email == submittedUser.Email);
                HttpContext.Session.SetInt32("Current_User_Id", current_user.UserId);
                int user_id = current_user.UserId;
                return Redirect($"home");
            }
            return View("Index");
        }


        [HttpGet("home")]
        public IActionResult Dashboard()
        {
            int? current_user_id = HttpContext.Session.GetInt32("Current_User_Id");
            if(current_user_id == null)
            {
                HttpContext.Session.Clear();
                return RedirectToAction("Index");
            }
            User current_user = dbContext.Users.FirstOrDefault(u => u.UserId == current_user_id);
            List<DojoActivity> AllActivities = dbContext.DojoActivities
                .Include(a => a.Coordinator)
                .Include(a => a.JoinedUsers)
                .ThenInclude(sub => sub.User)
                .OrderByDescending(a => a.ActivityDate)
                .ToList();
            List<User> AllUsers = dbContext.Users.ToList();

            ViewBag.Current_User = current_user;

            ViewBag.Current_User_Id = current_user_id;
            ViewBag.AllActivities = AllActivities;
            ViewBag.AllUsers = AllUsers;

            return View(current_user);
        }


        [HttpGet("activity/{dojoactivityId}")]
        public IActionResult DisplayActivity(int dojoactivityId)
        {
            int? current_user_id = HttpContext.Session.GetInt32("Current_User_Id");
            if(current_user_id == null)
            {
                HttpContext.Session.Clear();
                return RedirectToAction("Index");
            }
            User current_user = dbContext.Users.FirstOrDefault(u => u.UserId == current_user_id);
            List<DojoActivity> AllActivities = dbContext.DojoActivities
                .Include(a => a.Coordinator)
                .Include(a => a.JoinedUsers)
                .ThenInclude(sub => sub.User)
                .ToList();
            List<User> AllUsers = dbContext.Users.ToList();
            DojoActivity current_activity = dbContext.DojoActivities
                .Include(a => a.JoinedUsers)
                .ThenInclude(joined => joined.User)
                .FirstOrDefault(a => a.DojoActivityId == dojoactivityId);

            ViewBag.Current_User = current_user;
            ViewBag.Current_Activity = current_activity;
            ViewBag.Current_User_Id = current_user_id;
            ViewBag.AllActivities = AllActivities;
            ViewBag.AllUsers = AllUsers;
            return View();
        }


        [HttpGet("new")]
        public IActionResult DisplayForm()
        {
            int? current_user_id = HttpContext.Session.GetInt32("Current_User_Id");
            if(current_user_id == null)
            {
                HttpContext.Session.Clear();
                return RedirectToAction("Index");
            }
            User current_user = dbContext.Users.FirstOrDefault(u => u.UserId == current_user_id);
            List<DojoActivity> AllActivities = dbContext.DojoActivities
                .Include(a => a.Coordinator)
                .Include(a => a.JoinedUsers)
                .ThenInclude(sub => sub.User)
                .ToList();
            List<User> AllUsers = dbContext.Users.ToList();


            ViewBag.Current_User_Id = current_user_id;
            ViewBag.AllActivities = AllActivities;
            ViewBag.AllUsers = AllUsers;
            return View();
        }


        [HttpPost("newform")]
        public IActionResult PostForm(IndexViewModel modelData)
        {
            int? current_user_id = HttpContext.Session.GetInt32("Current_User_Id");
            if(current_user_id == null)
            {
                HttpContext.Session.Clear();
                return RedirectToAction("Index");
            }

            if(modelData == null)
            {
                return View("DisplayForm");
            }
            
            User current_user = dbContext.Users.FirstOrDefault(u => u.UserId == current_user_id);

            DojoActivity submittedActivity = modelData.newDojoActivity;
            submittedActivity.CoordinatorId = (int)current_user_id;
            submittedActivity.Coordinator = current_user;

            if(ModelState.IsValid)
            {
                dbContext.Add(submittedActivity);
                dbContext.SaveChanges();

                return Redirect($"activity/{submittedActivity.DojoActivityId}");
            }

            List<DojoActivity> AllActivities = dbContext.DojoActivities
                .Include(a => a.Coordinator)
                .Include(a => a.JoinedUsers)
                .ThenInclude(sub => sub.User)
                .ToList();
            List<User> AllUsers = dbContext.Users.ToList();


            ViewBag.Current_User_Id = current_user_id;
            ViewBag.AllActivities = AllActivities;
            ViewBag.AllUsers = AllUsers;
            return View("DisplayForm");
        }


        [HttpGet("delete/{dojoactivityId}")]
        public IActionResult Delete(int dojoactivityId)
        {
            int? current_user_id = HttpContext.Session.GetInt32("Current_User_Id");
            if(current_user_id == null)
            {
                HttpContext.Session.Clear();
                return RedirectToAction("Index");
            }

            DojoActivity current_activity = dbContext.DojoActivities.FirstOrDefault(a => a.DojoActivityId == dojoactivityId);
            dbContext.Remove(current_activity);
            dbContext.SaveChanges();

            return RedirectToAction("Dashboard");
        }


        [HttpGet("leave/{dojoactivityId}")]
        public IActionResult JoinActivity(int dojoactivityId)
        {
            int? current_user_id = HttpContext.Session.GetInt32("Current_User_Id");
            if(current_user_id == null)
            {
                HttpContext.Session.Clear();
                return RedirectToAction("Index");
            }
            Association current_association = dbContext.Associations
                .Where(a => a.ActivityId == dojoactivityId && a.UserId == current_user_id)
                .FirstOrDefault();
            
            dbContext.Remove(current_association);
            dbContext.SaveChanges();
            
            return RedirectToAction("Dashboard");
        }

        [HttpGet("join/{dojoactivityId}")]
        public IActionResult LeaveActivity(int dojoactivityId)
        {
            int? current_user_id = HttpContext.Session.GetInt32("Current_User_Id");
            if(current_user_id == null)
            {
                HttpContext.Session.Clear();
                return RedirectToAction("Index");
            }
            User current_user = dbContext.Users.FirstOrDefault(u => u.UserId == (int)current_user_id);
            DojoActivity current_activity = dbContext.DojoActivities.FirstOrDefault(a => a.DojoActivityId == dojoactivityId);

            Association newAssociation = new Association();
            newAssociation.UserId = (int)current_user_id;
            newAssociation.User = current_user;
            newAssociation.ActivityId = dojoactivityId;
            newAssociation.Activity = current_activity;

            dbContext.Add(newAssociation);
            dbContext.SaveChanges();

            return RedirectToAction("Dashboard");
        }

        [HttpGet("logout")]
        public IActionResult Logout()
        {
            HttpContext.Session.Clear();
            return RedirectToAction("Index");
        }

    }
}
