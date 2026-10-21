using System;
using System.Collections.Generic;
using System.Text;

namespace SaiiutUtsc.Application.Exceptions;

public class NoEncontradoException(string mensaje) : Exception(mensaje);
