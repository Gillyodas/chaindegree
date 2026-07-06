using System;
using System.Collections.Generic;
using System.Text;
using FluentValidation;

namespace ChainDegree.Core.Application.Degrees.Commands.IssueDegree
{
    public class IssueDegreeCommandValidator : AbstractValidator<IssueDegreeCommand>
    {
        public IssueDegreeCommandValidator()
        {
            RuleFor(x => x.Degrees).NotEmpty().WithMessage("Degrees list cannot be empty.");
            
            RuleForEach(x => x.Degrees).ChildRules(degree =>
            {
                degree.RuleFor(d => d.StudentId).NotEmpty().WithMessage("Student id is required.");
                degree.RuleFor(d => d.Major).NotEmpty().WithMessage("Major is required.");
                degree.RuleFor(d => d.Classification).NotEmpty().WithMessage("Classification is required.");
                degree.RuleFor(d => d.IssuedAt).NotEmpty().WithMessage("Issued date is required.");
            });
        }
    }
}
