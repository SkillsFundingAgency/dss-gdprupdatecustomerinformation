﻿using System.Collections.Generic;
using System;
using System.Data;
using System.Threading.Tasks;

namespace NCS.DSS.GDPRUpdateCustomerInformation.Service
{
    public interface IIdentifyAndAnonymiseDataService
    {
        Task AnonymiseData();
        Task DeleteCustomersFromCosmos(List<Guid> customerIdList);
        Task<List<Guid>> ReturnCustomerIds();
    }
}
