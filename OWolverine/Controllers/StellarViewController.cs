﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using OWolverine.Models;
using System.Net.Http;
using System.Xml.Linq;
using OWolverine.Models.Utility;
using OWolverine.Models.Ogame;
using OgameApiBLL;
using Microsoft.AspNetCore.Identity;
using OWolverine.Data;
using Microsoft.EntityFrameworkCore;
using OWolverine.Services.Ogame;
using CSharpUtilities;
using OWolverine.Models.StarMapViewModels;

namespace OWolverine.Controllers
{
    public class StellarViewController : Controller
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly SignInManager<ApplicationUser> _signInManager;
        private readonly ApplicationDbContext _context;

        private const string playerAPI = "players.xml";
        private const string universeAPI = "universe.xml";
        private const string playerDataApi = "playerData.xml";

        /// <summary>
        /// Dependency Injection
        /// </summary>
        /// <param name="context"></param>
        /// <param name="userManager"></param>
        /// <param name="signInManager"></param>
        public StellarViewController(
            ApplicationDbContext context,
            UserManager<ApplicationUser> userManager,
            SignInManager<ApplicationUser> signInManager)
        {
            _context = context;
            _userManager = userManager;
            _signInManager = signInManager;
        }

        /// <summary>
        /// Get server list and random target on Index
        /// </summary>
        /// <returns></returns>
        public async Task<IActionResult> Index()
        {
            return View(new StarIndexViewModel(await _context.Universes
                .Include(u => u.Players)
                .ToArrayAsync()));
        }

        /// <summary>
        /// Search player from database
        /// </summary>
        /// <param name="vm"></param>
        /// <returns></returns>
        public async Task<IActionResult> Search(StarSearchViewModel vm)
        {
            var sivm = new StarIndexViewModel(await _context.Universes
                .Include(u => u.Players)
                .ToArrayAsync());
            if (ModelState.IsValid)
            {
                sivm.Players = _context.Universes
                    .Include(u => u.Players)
                        .ThenInclude(player => player.Planets)
                        .ThenInclude(planet => planet.Moon)
                    .First(u => u.Id == vm.ServerId)
                    .Players
                    .Where(p => p.Name.Contains(vm.PlayerName)).ToList();
            }
            return View("Index", sivm);
        }

        public async Task<IActionResult> RefreshUniverse(int id)
        {
            var universe = _context.Universes.FirstOrDefault(u => u.Id == id);
            if (universe == null)
            {
                return NotFound();
            }

            //Load players into DB
            var playersData = OgameApi.GetAllPlayers(id);
            var playerList = playersData.Players;
            var playersLastUpdate = DateTimeHelper.UnixTimeStampToDateTime(playersData.LastUpdate);
            if (playersLastUpdate != universe.PlayersLastUpdate)
            {
                //Only update if the API Date is different
                var removeList = new List<Player>();
                foreach(var player in universe.Players)
                {
                    var pNew = playerList.FirstOrDefault(p => p.PlayerId == player.PlayerId);
                    if(pNew == null)
                    {
                        //Remove player if not found in API
                        removeList.Add(player);
                    }
                    else
                    {
                        player.Update(pNew);
                        playerList.Remove(pNew); //Remove from list after update
                    }
                }
                _context.RemoveRange(removeList);
                _context.AddRange(playerList);
                universe.PlayersLastUpdate = playersLastUpdate; //Update API Date
                await _context.SaveChangesAsync();
            }

            //Load Alliance into DB
            var allianceData = OgameApi.GetAllAlliances(id);
            var allianceList = allianceData.Alliances;
            var alliancesLastUpdate = DateTimeHelper.UnixTimeStampToDateTime(allianceData.LastUpdate);
            if (alliancesLastUpdate != universe.AllianceLastUpdate)
            {
                //Only update if the API Date is different
                allianceList.ForEach(a =>
                {
                    var dbItem = _context.Alliances.FirstOrDefault(e => e.AllianceId == a.AllianceId && e.ServerId == a.ServerId);
                    a.Founder = universe.Players.FirstOrDefault(p => p.PlayerId == a.FounderId);
                    if (a.Founder == null)
                    {
                        //Unset founder ID if founder not found
                        a.FounderId = null;
                    }
                    else
                    {
                        //Assign ID of player in DB
                        a.FounderId = a.Founder.Id;
                    }

                    for (var i = 0; i < a.Members.Count; i++)
                    {
                        //Replace member placeholder with database object
                        a.Members[i] = universe.Players.FirstOrDefault(p => p.PlayerId == a.Members[i].PlayerId);
                    }

                    if (dbItem != null)
                    {
                        //Assign ID if exists in database
                        a.Id = dbItem.Id;
                        //Update the context
                        dbItem.Update(a);
                    }
                });
                //Remove alliances no longer exists
                universe.Alliances.RemoveAll(e => !allianceList.Any(a => a.Id == e.Id));
                _context.AddRange(allianceList.Where(a => a.Id == 0)); //Add new alliances
                universe.AllianceLastUpdate = alliancesLastUpdate; //Update API Date
                await _context.SaveChangesAsync();
            }

            //Load Planet into DB
            var planetData = OgameApi.GetAllPlanets(id);
            var planetList = planetData.Planets;
            var planetLastUpdate = DateTimeHelper.UnixTimeStampToDateTime(planetData.LastUpdate);
            var removePlanetList = new List<Planet>();
            if (planetLastUpdate != universe.PlanetsLastUpdate)
            {
                //Only update if the API Date is different
                //Update existing planets
                var removeList = new List<Planet>();
                foreach(var planet in universe.Planets)
                {
                    var pNew = planetList.FirstOrDefault(p => p.PlanetId == planet.PlanetId && p.ServerId == planet.ServerId);
                    if (pNew == null)
                    {
                        //Remove if not exists in API
                        removeList.Add(planet);
                    }
                    else
                    {
                        planet.Update(pNew);
                        planetList.Remove(pNew); //Remove updated item
                    }
                }
                _context.RemoveRange(removeList);
                await _context.SaveChangesAsync();

                planetList.ForEach(p =>
                {
                    //Assign real owner ID
                    var owner = universe.Players.FirstOrDefault(e => e.PlayerId == p.OwnerId);
                    if (owner == null) {
                        //Player removed planet no longer exists
                        removePlanetList.Add(p);
                    }
                    else
                    {
                        p.OwnerId = owner.Id;
                    }
                });
                //Remove planets with player deleted
                planetList.RemoveAll(p => removePlanetList.Contains(p));

                //Remove planets no longer exists
                _context.AddRange(planetList);
                universe.PlanetsLastUpdate = planetLastUpdate;
            }
            universe.LastUpdate = DateTime.Now;
            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        public JsonResult GetUser(string server,string playerName)
        {
            var player = OgameAPI.FindPlayer(server, playerName);
            if(player == null)
            {
                return new JsonResult(new Response() { status = APIStatus.Fail, message = "玩家不存在" });
            }

            OgameAPI.FillPlayerData(player);            
            return new JsonResult(new Response() {
                status = APIStatus.Success,
                message = "Returned with data",
                data = new[] { player.Data }
            });
        }

        [HttpGet]
        public JsonResult GetServers()
        {
            var httpClient = new HttpClient();
            var result = httpClient.GetAsync("https://s101-tw.ogame.gameforge.com/api/universes.xml").Result;
            var stream = result.Content.ReadAsStreamAsync().Result;
            var itemXml = XElement.Load(stream);
            var servers = itemXml.Elements("universe");
            return new JsonResult(false);
        }

        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}
