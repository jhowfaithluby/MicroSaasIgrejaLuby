using FluentValidation;

namespace MicroSaas.Application.Members.Commands.CreateMember;

public sealed class CreateMemberCommandValidator : AbstractValidator<CreateMemberCommand>
{
    public CreateMemberCommandValidator()
    {
        RuleFor(x => x.FullName)
            .NotEmpty()
            .MinimumLength(3)
            .MaximumLength(150);

        RuleFor(x => x.Email)
            .NotEmpty()
            .EmailAddress();

        RuleFor(x => x.Phone)
            .NotEmpty()
            .Matches("^\\+?[0-9]{8,15}$")
            .WithMessage("O telefone deve conter entre 8 e 15 dígitos e pode incluir o código do país");
    }
}
