using System;
using System.Collections.Generic;
using System.Text;

namespace UniEvalUtsc.Application.Exceptions;

public class NoEncontradoException(string mensaje) : Exception(mensaje);
