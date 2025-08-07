using System;

namespace Ledon.BerryShare.Shared.Base;

public class DtoBase
{

}


public interface IPaged
{
    int PageIndex { get; set; }
    int PageSize { get; set; }
}