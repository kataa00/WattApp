﻿using Microsoft.AspNetCore.Components;
using prosumerAppBack.Models.Dispatcher;
using Dispatcher = prosumerAppBack.Models.Dispatcher.Dispatcher;

namespace prosumerAppBack.DataAccess.DispatcherRep;

public interface IDispatcherRepository
{
    Task<Dispatcher> GetDispatcherByIdAsync(Guid id);
    Task<Dispatcher> GetUserByEmailAndPasswordAsync(string email, string password);
    Task<Boolean> UpdatePassword(Guid id, string newPassword);
    Task<Dispatcher> CreateDispatcher(DispatcherRegisterDto dispatcherRegisterDto);
    Task<Dispatcher> GetDispatcherByEmailAsync(string email);
    Task<Dispatcher> GetDispatcherByUsernameAsync(string username);
}