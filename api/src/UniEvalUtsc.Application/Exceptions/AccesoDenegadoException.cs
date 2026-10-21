using System;
using System.Collections.Generic;
using System.Text;

namespace UniEvalUtsc.Application.Exceptions;

public class AccesoDenegadoException(string mensaje) : Exception(mensaje);
