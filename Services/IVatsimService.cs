using System;
using System.Collections.Generic;
using vTFMS.Models;

namespace vTFMS.Services;

public interface IVatsimService
{
    event EventHandler<List<VatsimPilot>>? PilotsUpdated;
    void Start();
    void Stop();
    List<VatsimPilot> CurrentPilots { get; }
}