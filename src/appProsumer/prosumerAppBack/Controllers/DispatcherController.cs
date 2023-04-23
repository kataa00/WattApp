﻿using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using prosumerAppBack.BusinessLogic.DispatcherService;
using prosumerAppBack.Helper;
using prosumerAppBack.Models.Dispatcher;
using System.Data;
using System.Text.Json;

namespace prosumerAppBack.Controllers;

[ApiController]
[Route("api/[controller]")]
public class DispatcherController : ControllerBase
{
    private readonly IDispatcherService _dispatcherService;
    private readonly ITokenMaker _tokenMaker;
    public DispatcherController(ITokenMaker tokenMaker, IDispatcherService dispatcherService)
    {
        _dispatcherService = dispatcherService;
        _tokenMaker = tokenMaker;
    }

    [HttpPost("signup")]
    //[Authorize(Roles = "Admin")]
    public async Task<IActionResult> Register([FromBody] DispatcherRegisterDto userRegisterDto)
    {
        try
        {
            await _dispatcherService.CheckEmail(userRegisterDto.Email);
            await _dispatcherService.CheckUsername(userRegisterDto.Username);

            await _dispatcherService.CreateDispatcher(userRegisterDto);

            return Ok(new { message = "User registered successfully" });
        }
        catch (ArgumentNullException ex)
        {
            throw new ArgumentException(ex.Message);
        }
    }

    [HttpPost("signin")]
    [AllowAnonymous]
    public async Task<IActionResult> Login([FromBody] DispatcherLoginDto dispatcherLoginDto)
    {
        try
        {
            var user = await _dispatcherService.GetUserByEmailAndPasswordAsync(dispatcherLoginDto.Email, dispatcherLoginDto.Password);

            var token = _tokenMaker.GenerateToken(user);
            return Ok(JsonSerializer.Serialize(token));
        }
        catch (ArgumentNullException ex)
        {
            throw new ArgumentException(ex.Message);
        }
    }

    [HttpPost("validate-token")]
    //[Authorize(Roles = "Dispatcher,Admin")]
    public ActionResult<object> ValidateToken([FromBody] object body)
    {
        string token = body.ToString();
        var result = _tokenMaker.ValidateJwtToken(token);

        if (!result)
        {
            return BadRequest("Invalid token");
        }

        return true;
    }

    [HttpGet("get-all-dispatchers")]
    [Authorize(Roles = "Dispatcher,Admin")]
    public async Task<IActionResult> AllUsersInfo()
    {
        try
        {
            var results = await _dispatcherService.GetAllDispatchersAsync();

            return Ok(results);
        }
        catch (Exception ex)
        {
            return StatusCode(500, ex.Message);
        }
    }
}

