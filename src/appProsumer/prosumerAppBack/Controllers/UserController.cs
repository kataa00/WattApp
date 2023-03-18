﻿using prosumerAppBack.Models;
using System;
using System.Text.Json;
using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using prosumerAppBack.Helper;
using Microsoft.EntityFrameworkCore;
using prosumerAppBack.BusinessLogic;

namespace prosumerAppBack.Controllers;

[ApiController]
[Route("api/[controller]")]
public class UserController : ControllerBase
{
    private readonly IUserRepository _userRepository;
    private readonly ITokenMaker _tokenMaker;
    private readonly IUserService _userService;
    private readonly IEmailService _emailService;

    public UserController(IUserRepository userRepository,ITokenMaker tokenMaker, IUserService userService, EmailService emailService)
    {
        _userRepository = userRepository;
        _tokenMaker = tokenMaker;
        _userService = userService;
        _emailService = emailService;
    }

    [HttpGet("username")]
    public ActionResult<string> GetData()
    {
        var id = _userService.GetID();
        var username = _userRepository.GetUsernameByIdAsync(id);
        return Ok(JsonSerializer.Serialize(username));
    }
    [HttpPost("signup")]
    public async Task<IActionResult> Register([FromBody] UserRegisterDto userRegisterDto)
    {
        var user = await _userRepository.GetUserByEmailAsync(userRegisterDto.Email);
        if (user != null)
        {
            return BadRequest("email already exist");
        }

        await _userRepository.CreateUser(userRegisterDto);

        return Ok(new { message = "User registered successfully" });
    }

    [HttpPost("signin")]
    public async Task<IActionResult> Login([FromBody] UserLoginDto userLoginDto)
    {
        var user = await _userRepository.GetUserByEmailAndPasswordAsync(userLoginDto.Email, userLoginDto.Password);
        if (user == null)
        {
            return BadRequest("Invalid email or password");
        }

        var token = _tokenMaker.GenerateToken(user);
        return Ok( JsonSerializer.Serialize(token) );
    }

    [HttpGet("users"),Authorize(Roles = "RegularUser")]
    public async Task<IActionResult> GetUsers()
    {
        var users = await _userRepository.GetAllUsers();
        return Ok(users);
    }
    [HttpPost("validate-token")]
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

    [HttpGet("users/{id}")]
    public async Task<ActionResult<User>> GetUser(int id)
    {
        var user = await _userRepository.GetUserByIdAsync(id);
        if(user == null)
        {
            return BadRequest("User not found");
        }

        return Ok(user);
    }

    [HttpGet("users")]
    public async Task<IActionResult> GetUsers()
    {
        var users = await _userRepository.GetUsersAsync();

        if(users == null)
        {
            return BadRequest("Theres no users in the database");
        }

        return Ok(users);
    }

    [HttpPost("users/{id}")]
    public async Task<IActionResult> UpdateUser(int id, [FromBody] UserUpdateDto userUpdateDto)
    {
        var user = _userRepository.UpdateUser(id, userUpdateDto);

        if(user == null)
        {
            return BadRequest("cannot update user");
        }

        return Ok(new { message = "user updated successfully" });
    }

    [HttpPost("send-reset-email")]
    public async Task<IActionResult> SendResetEmail([FromBody] ResetPasswordEmailDto resetPasswordEmailDto)
    {
        var user = await _userRepository.GetUserByEmailAsync(resetPasswordDto.Email);

        if (user == null)
        {
            return BadRequest("Invalid email address");
        }

        var token = _tokenMaker.GenerateToken(user);
        var resetPasswordUrl = $"https://localhost:7182/api/user/reset-password?token={token}";
        var message = $"Please click the following link to reset your password: {resetPasswordUrl}";
        await _emailService.SendEmailAsync(user.Email, "Reset password", message);

        return Ok(new { message = "Reset password link has been sent to your email" });
    }

    [HttpPost("reset-password")]
    public async Task<IActionResult> ResetPassword([FromQuery] string token [FromBody] ResetPasswordDto resetPasswordDto)
    {
        (int?, string?) result = _tokenMaker.ValidateToken(token);

        if (result.Item1 == null)
        {
            return BadRequest("Invalid token");
        }

        Task<User> user = _userRepository.GetUserByIdAsync((int)result.Item1);

        if (user == null)
        {
            return BadRequest("User not found" + user.Id);
        }

        var userCheck = _userRepository.GetUserByEmailAsync(resetPasswordDto.Email);

        if (userCheck == null || resetPasswordDto.Email != user.Email)
        {
            return BadRequest("Invalid email address");
        }

        Boolean action = _userRepository.UpdatePassword(user.Id, resetPasswordDto.Password);

        if (!action)
        {
            return BadRequest("Action failed");
        }

        return Ok(new { message = "Password changed" });
    }
}