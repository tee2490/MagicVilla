﻿using MagicVilla_ClassLibrary.Models;
using MagicVilla_ClassLibrary.Models.Dto;
using MagicVilla_VillaAPI.Data;
using Microsoft.AspNetCore.Mvc;

namespace MagicVilla_VillaAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class VillaAPIController : ControllerBase
    {

        [HttpGet]
        public IEnumerable<Villa> GetVillas()
        {
            return new List<Villa> {
             new Villa{Id=1,Name="Pool View" },
             new Villa{Id=2,Name="Beach View" }
             };
        }

        [HttpGet("{id:int}")]
        public VillaDTO GetVilla(int id)
        {
            return VillaStore.villaList.FirstOrDefault(u => u.Id == id);
        }
    }
}
