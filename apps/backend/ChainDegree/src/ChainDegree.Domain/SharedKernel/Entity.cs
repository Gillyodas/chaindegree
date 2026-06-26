using System;
using System.Collections.Generic;
using System.Text;
using ChainDegree.Core.Domain.SharedKernel.Interfaces;

namespace ChainDegree.Core.Domain.SharedKernel
{
    public abstract class Entity
    {
        private readonly List<IDomainEvent> _domainEvents = new();

        // Chỉ đọc từ bên ngoài
        public IReadOnlyCollection<IDomainEvent> DomainEvents => _domainEvents.AsReadOnly();

        protected void AddDomainEvent(IDomainEvent domainEvent)
        {
            _domainEvents.Add(domainEvent);
        }

        public void ClearDomainEvents()
        {
            _domainEvents.Clear();
        }
    }
}
