import React, { useState, useCallback } from "react";

/**
 * Formulario de registro de entidad - Módulo administrativo
 *
 * Endpoint: POST /api/entidades
 * Envío como multipart/form-data (incluye dos adjuntos PDF).
 */

const MAX_FILE_SIZE_MB = 5;
const MAX_FILE_SIZE_BYTES = MAX_FILE_SIZE_MB * 1024 * 1024;

const initialFormState = {
  nit: "",
  nombreEntidad: "",
  ipPublica: "",
  nombreEnlaceTecnico: "",
  correoResponsable: "",
};

const initialFiles = {
  autorizacionInstitucional: null,
  resolucionHabilitante: null,
};

const initialTouched = {
  nit: false,
  nombreEntidad: false,
  ipPublica: false,
  nombreEnlaceTecnico: false,
  correoResponsable: false,
  autorizacionInstitucional: false,
  resolucionHabilitante: false,
};

/**
 * Calcula el dígito de verificación de un NIT colombiano
 * usando el algoritmo oficial (módulo 11 con vector de pesos DIAN).
 */
function calcularDigitoVerificacion(numeroBase) {
  const pesos = [
    71, 67, 59, 53, 47, 43, 41, 37, 29, 23, 19, 17, 13, 7, 3,
  ];
  const digitos = numeroBase.split("").reverse();
  const pesosRecortados = pesos.slice(pesos.length - digitos.length);

  let suma = 0;
  for (let i = 0; i < digitos.length; i++) {
    suma += parseInt(digitos[i], 10) * pesosRecortados[i];
  }

  const residuo = suma % 11;
  if (residuo === 0 || residuo === 1) return residuo;
  return 11 - residuo;
}

function validarNIT(valor) {
  if (!valor.trim()) return "Ingresa el número de identificación fiscal.";

  const limpio = valor.trim();
  const patron = /^(\d{5,15})-(\d)$/;
  const coincide = limpio.match(patron);

  if (!coincide) {
    return "Usa el formato número-dígito, por ejemplo 900123456-7.";
  }

  const [, base, digitoIngresado] = coincide;
  const digitoCalculado = calcularDigitoVerificacion(base);

  if (parseInt(digitoIngresado, 10) !== digitoCalculado) {
    return "El dígito de verificación no coincide con el número ingresado. Revísalo.";
  }

  return "";
}

function validarNombreEntidad(valor) {
  if (!valor.trim()) return "Ingresa el nombre oficial de la entidad.";
  if (valor.trim().length < 3) {
    return "El nombre es demasiado corto. Ingresa el nombre completo.";
  }
  return "";
}

function validarIPv4(valor) {
  if (!valor.trim()) return "Ingresa la dirección IP pública del servidor.";

  const partes = valor.trim().split(".");
  if (partes.length !== 4) {
    return "La dirección IP debe tener el formato 192.168.1.1.";
  }

  for (const parte of partes) {
    if (!/^\d{1,3}$/.test(parte)) {
      return "La dirección IP solo debe contener números separados por puntos.";
    }
    const num = parseInt(parte, 10);
    if (num < 0 || num > 255) {
      return "Cada grupo de la dirección IP debe estar entre 0 y 255.";
    }
    if (parte.length > 1 && parte.startsWith("0")) {
      return "No uses ceros a la izquierda en la dirección IP.";
    }
  }

  return "";
}

function validarNombreEnlace(valor) {
  if (!valor.trim()) return "Ingresa el nombre del enlace técnico designado.";
  if (valor.trim().length < 3) {
    return "Ingresa el nombre completo del enlace técnico.";
  }
  return "";
}

function validarCorreo(valor) {
  if (!valor.trim()) {
    return "Ingresa el correo del responsable de protección de datos.";
  }
  const patron = /^[^\s@]+@[^\s@]+\.[^\s@]+$/;
  if (!patron.test(valor.trim())) {
    return "Ingresa un correo electrónico válido, por ejemplo nombre@entidad.gov.co.";
  }
  return "";
}

function validarArchivo(archivo, etiqueta) {
  if (!archivo) return `Adjunta el archivo: ${etiqueta}.`;
  if (archivo.type !== "application/pdf") {
    return "El archivo debe estar en formato PDF.";
  }
  if (archivo.size > MAX_FILE_SIZE_BYTES) {
    return `El archivo supera el tamaño máximo permitido de ${MAX_FILE_SIZE_MB} MB.`;
  }
  return "";
}

function formatearTamano(bytes) {
  if (bytes < 1024) return `${bytes} B`;
  if (bytes < 1024 * 1024) return `${(bytes / 1024).toFixed(1)} KB`;
  return `${(bytes / (1024 * 1024)).toFixed(2)} MB`;
}

export default function RegistroEntidadForm() {
  const [form, setForm] = useState(initialFormState);
  const [files, setFiles] = useState(initialFiles);
  const [touched, setTouched] = useState(initialTouched);
  const [submitting, setSubmitting] = useState(false);
  const [submitStatus, setSubmitStatus] = useState(null); // { type: 'success' | 'error', message }

  const validators = {
    nit: validarNIT,
    nombreEntidad: validarNombreEntidad,
    ipPublica: validarIPv4,
    nombreEnlaceTecnico: validarNombreEnlace,
    correoResponsable: validarCorreo,
  };

  const fileLabels = {
    autorizacionInstitucional: "Documento de autorización institucional",
    resolucionHabilitante: "Resolución o acto administrativo habilitante",
  };

  const errors = {
    nit: validators.nit(form.nit),
    nombreEntidad: validators.nombreEntidad(form.nombreEntidad),
    ipPublica: validators.ipPublica(form.ipPublica),
    nombreEnlaceTecnico: validators.nombreEnlaceTecnico(form.nombreEnlaceTecnico),
    correoResponsable: validators.correoResponsable(form.correoResponsable),
    autorizacionInstitucional: validarArchivo(
      files.autorizacionInstitucional,
      fileLabels.autorizacionInstitucional
    ),
    resolucionHabilitante: validarArchivo(
      files.resolucionHabilitante,
      fileLabels.resolucionHabilitante
    ),
  };

  const formEsValido = Object.values(errors).every((e) => e === "");

  const handleChange = (campo) => (e) => {
    const valor = e.target.value;
    setForm((prev) => ({ ...prev, [campo]: valor }));
  };

  const handleBlur = (campo) => () => {
    setTouched((prev) => ({ ...prev, [campo]: true }));
  };

  const handleFileChange = (campo) => (e) => {
    const archivo = e.target.files && e.target.files[0] ? e.target.files[0] : null;
    setFiles((prev) => ({ ...prev, [campo]: archivo }));
    setTouched((prev) => ({ ...prev, [campo]: true }));
  };

  const marcarTodoComoTocado = useCallback(() => {
    setTouched({
      nit: true,
      nombreEntidad: true,
      ipPublica: true,
      nombreEnlaceTecnico: true,
      correoResponsable: true,
      autorizacionInstitucional: true,
      resolucionHabilitante: true,
    });
  }, []);

  const handleSubmit = async (e) => {
    e.preventDefault();
    setSubmitStatus(null);

    if (!formEsValido) {
      marcarTodoComoTocado();
      setSubmitStatus({
        type: "error",
        message: "Revisa los campos marcados en rojo antes de continuar.",
      });
      return;
    }

    setSubmitting(true);

    try {
      const payload = new FormData();
      payload.append("nit", form.nit.trim());
      payload.append("nombreEntidad", form.nombreEntidad.trim());
      payload.append("ipPublica", form.ipPublica.trim());
      payload.append("nombreEnlaceTecnico", form.nombreEnlaceTecnico.trim());
      payload.append("correoResponsable", form.correoResponsable.trim());
      payload.append("autorizacionInstitucional", files.autorizacionInstitucional);
      payload.append("resolucionHabilitante", files.resolucionHabilitante);

      const response = await fetch("/api/entidades", {
        method: "POST",
        body: payload,
      });

      if (!response.ok) {
        let mensaje = "No fue posible registrar la entidad. Intenta nuevamente.";
        try {
          const data = await response.json();
          if (data && data.message) mensaje = data.message;
        } catch (_) {
          // el servidor no devolvió JSON, se usa el mensaje por defecto
        }
        setSubmitStatus({ type: "error", message: mensaje });
        return;
      }

      setSubmitStatus({
        type: "success",
        message: "La entidad se registró correctamente.",
      });
      setForm(initialFormState);
      setFiles(initialFiles);
      setTouched(initialTouched);
    } catch (err) {
      setSubmitStatus({
        type: "error",
        message:
          "No se pudo conectar con el servidor. Verifica tu conexión e intenta de nuevo.",
      });
    } finally {
      setSubmitting(false);
    }
  };

  const campoTexto = ({
    id,
    label,
    placeholder,
    tipo = "text",
    ayuda,
  }) => {
    const mostrarError = touched[id] && errors[id];
    return (
      <div className="mb-5">
        <label htmlFor={id} className="block text-sm font-medium text-slate-700 mb-1">
          {label} <span className="text-red-600">*</span>
        </label>
        <input
          id={id}
          type={tipo}
          value={form[id]}
          onChange={handleChange(id)}
          onBlur={handleBlur(id)}
          placeholder={placeholder}
          className={`w-full rounded-md border px-3 py-2 text-sm shadow-sm outline-none transition-colors
            focus:ring-2 focus:ring-offset-0
            ${
              mostrarError
                ? "border-red-400 focus:border-red-500 focus:ring-red-200"
                : "border-slate-300 focus:border-blue-500 focus:ring-blue-200"
            }`}
          aria-invalid={Boolean(mostrarError)}
          aria-describedby={mostrarError ? `${id}-error` : undefined}
        />
        {ayuda && !mostrarError && (
          <p className="mt-1 text-xs text-slate-500">{ayuda}</p>
        )}
        {mostrarError && (
          <p id={`${id}-error`} className="mt-1 text-xs text-red-600">
            {errors[id]}
          </p>
        )}
      </div>
    );
  };

  const campoArchivo = (id) => {
    const archivo = files[id];
    const mostrarError = touched[id] && errors[id];
    return (
      <div className="mb-5">
        <label htmlFor={id} className="block text-sm font-medium text-slate-700 mb-1">
          {fileLabels[id]} <span className="text-red-600">*</span>
        </label>
        <div
          className={`rounded-md border border-dashed px-3 py-3 text-sm
            ${mostrarError ? "border-red-400 bg-red-50" : "border-slate-300 bg-slate-50"}`}
        >
          <input
            id={id}
            type="file"
            accept="application/pdf"
            onChange={handleFileChange(id)}
            className="block w-full text-sm text-slate-700 file:mr-3 file:rounded-md file:border-0
              file:bg-blue-600 file:px-3 file:py-1.5 file:text-sm file:font-medium file:text-white
              hover:file:bg-blue-700"
          />
          {archivo && !mostrarError && (
            <p className="mt-2 text-xs text-slate-600">
              {archivo.name} · {formatearTamano(archivo.size)}
            </p>
          )}
        </div>
        <p className="mt-1 text-xs text-slate-500">
          Formato PDF, tamaño máximo {MAX_FILE_SIZE_MB} MB.
        </p>
        {mostrarError && <p className="mt-1 text-xs text-red-600">{errors[id]}</p>}
      </div>
    );
  };

  return (
    <div className="min-h-screen bg-slate-100 py-10 px-4">
      <div className="mx-auto max-w-2xl rounded-lg bg-white shadow-sm border border-slate-200">
        <div className="border-b border-slate-200 px-6 py-5">
          <h1 className="text-xl font-semibold text-slate-800">
            Registro de nueva entidad
          </h1>
          <p className="mt-1 text-sm text-slate-500">
            Módulo administrativo · Completa todos los campos para registrar la entidad en el sistema.
          </p>
        </div>

        <form onSubmit={handleSubmit} className="px-6 py-6" noValidate>
          {campoTexto({
            id: "nit",
            label: "Número de identificación fiscal",
            placeholder: "Ej. 900123456-7",
            ayuda: "Incluye el dígito de verificación separado por un guion.",
          })}

          {campoTexto({
            id: "nombreEntidad",
            label: "Nombre oficial de la entidad",
            placeholder: "Ej. Ministerio de Ejemplo",
          })}

          {campoTexto({
            id: "ipPublica",
            label: "Dirección IP pública del servidor de consumo",
            placeholder: "Ej. 190.24.10.5",
            ayuda: "Formato IPv4, cuatro grupos de números separados por puntos.",
          })}

          {campoTexto({
            id: "nombreEnlaceTecnico",
            label: "Nombre del enlace técnico designado",
            placeholder: "Ej. Juan Pérez",
          })}

          {campoTexto({
            id: "correoResponsable",
            label: "Correo del responsable de protección de datos",
            placeholder: "Ej. proteccion.datos@entidad.gov.co",
            tipo: "email",
          })}

          <div className="my-6 border-t border-slate-200" />

          {campoArchivo("autorizacionInstitucional")}
          {campoArchivo("resolucionHabilitante")}

          {submitStatus && (
            <div
              className={`mb-5 rounded-md px-4 py-3 text-sm ${
                submitStatus.type === "success"
                  ? "bg-green-50 text-green-700 border border-green-200"
                  : "bg-red-50 text-red-700 border border-red-200"
              }`}
              role="alert"
            >
              {submitStatus.message}
            </div>
          )}

          <button
            type="submit"
            disabled={submitting}
            className={`w-full rounded-md px-4 py-2.5 text-sm font-medium text-white transition-colors
              ${
                submitting
                  ? "bg-blue-400 cursor-not-allowed"
                  : "bg-blue-600 hover:bg-blue-700"
              }`}
          >
            {submitting ? "Registrando entidad..." : "Registrar entidad"}
          </button>
        </form>
      </div>
    </div>
  );
}
