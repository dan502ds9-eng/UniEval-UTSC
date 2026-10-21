using System;
using System.Collections.Generic;
using System.Text;

namespace UniEvalUtsc.Domain.Exceptions;

public class DomainException(string mensaje) : Exception(mensaje);