using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using qsLibPack.Application;
using qsLibPack.Domain.Exceptions;
using qsLibPack.Repositories.Interfaces;
using qsLibPack.Validations.Interface;
using qsLog.Applications.Models;
using qsLog.Applications.Services.Interfaces;
using qsLog.Domains.Users;
using qsLog.Domains.Users.Repository;
using qsLibPack.Domain.ValueObjects;

namespace qsLog.Applications.Services.Users
{
    public class UserService : ApplicationService, IUserService
    {
        readonly IUserRepository _userRepository;
        public UserService(IValidationService validationService, IUnitOfWork uow, IUserRepository userRepository) : base(validationService, uow)
        {
            _userRepository = userRepository;
        }

        public async Task Create(UserModel model)
        {
             if (!model.IsValid())
            {
                _validationService.AddErrors(model.Errors);
                return;
            }

            if (_userRepository.ExistsUserName(model.UserName))
            {
                _validationService.AddErrors("UserName", "Login já informado. Por favor, informe outro.");
                return;
            }

            try
            {
                var user = new User(model.Name, model.UserName, model.Email, new qsLibPack.Domain.ValueObjects.PasswordVO(model.Password, model.ConfirmPassword), model.Administrator);
                await _userRepository.CreateAsync(user);
                await _uow.CommitAsync();
            }
            catch (DomainException dx)
            {
                _validationService.AddErrors("U01", dx.Message);
            }
        }

        public IList<UserListModel> ListAll()
        {
            var users = _userRepository.ListAll();
            return users.Select(u => new UserListModel
            {
                Id = u.Id,
                Name = u.Name,
                Email = u.Email,
                UserName = u.UserName,
                Administrator = u.Administrator
            }).ToList();
        }

        public async Task<UserModel> GetByID(Guid id)
        {
            var u = await _userRepository.GetByIDAsync(id);
            if (u == null) return new UserModel();

            return new UserModel
            {
                Id = u.Id,
                Name = u.Name,
                Email = u.Email,
                UserName = u.UserName,
                Administrator = u.Administrator
            };
        }

        public Task<IList<UserListModel>> GetByName(string name)
        {
            throw new NotImplementedException();
        }

        public async Task Update(UserModel model, Guid id)
        {
            var user = await _userRepository.GetByIDAsync(id);
            if (user == null) return;

            if (model.UserName != user.UserName)
            {
                 if (_userRepository.ExistsUserName(model.UserName))
                {
                    _validationService.AddErrors("UserName", "Login já informado. Por favor, informe outro.");
                    return;
                }
            }

            try
            {
                user.SetName(model.Name);
                user.SetEmail(model.Email);
                user.SetAdministrator(model.Administrator);
                user.SetUserName(model.UserName);

                _userRepository.Update(user);
                _uow.Commit();
            }
            catch (DomainException dx)
            {
                _validationService.AddErrors("U01", dx.Message);
            }
        }

        public async Task ChangePassoword(Guid id, string oldPassword, PasswordVO newPassword)
        {
            var user = await _userRepository.GetByIDAsync(id);
            if (user == null) return;

            try
            {
                user.ChangePassoword(oldPassword, newPassword);
                _userRepository.Update(user);
                _uow.Commit();
            }
            catch (DomainException dx)
            {
                _validationService.AddErrors("ChangePassword", dx.Message);
            }
        }

        public async Task ResetPassword(Guid id)
        {
            var user = await _userRepository.GetByIDAsync(id);
            if (user == null) return;

            try
            {
                user.ResetPassword();
                _userRepository.Update(user);
                _uow.Commit();
            }
            catch (DomainException dx)
            {
                _validationService.AddErrors("ResetPassoword", dx.Message);
            }
        }
    }
}