function soloDigitos(valor) {
    return (valor || "").replace(/\D/g, "");
}

function formatearNit(valor) {
    const digitos = soloDigitos(valor).slice(0, 9);

    if (digitos.length <= 1) {
        return digitos;
    }

    return digitos.slice(0, -1) + "-" + digitos.slice(-1);
}

function formatearTelefono(valor) {
    const digitos = soloDigitos(valor).slice(0, 8);

    if (digitos.length <= 4) {
        return digitos;
    }

    return digitos.slice(0, 4) + "-" + digitos.slice(4);
}

function normalizarDecimalTexto(valor) {
    let limpio = (valor || "")
        .toString()
        .replace(",", ".")
        .replace(/[^0-9.]/g, "");

    const partes = limpio.split(".");

    if (partes.length > 2) {
        limpio = partes[0] + "." + partes.slice(1).join("");
    }

    if (limpio.includes(".")) {
        const partesDecimal = limpio.split(".");
        const entero = partesDecimal[0] || "0";
        const decimal = partesDecimal[1] ?? "";

        limpio = entero + "." + decimal.slice(0, 2);
    }

    return limpio;
}

function normalizarDecimalFinal(valor) {
    let limpio = normalizarDecimalTexto(valor);

    if (!limpio || limpio === "." || limpio === "0.") {
        return "";
    }

    const numero = Number(limpio);

    if (Number.isNaN(numero)) {
        return "";
    }

    return numero.toFixed(2);
}

document.addEventListener("beforeinput", function (e) {
    const input = e.target;

    if (!(input instanceof HTMLInputElement)) {
        return;
    }

    if (input.dataset.decimalPositivo !== "true") {
        return;
    }

    if (e.data === ",") {
        e.preventDefault();

        const inicio = input.selectionStart ?? input.value.length;
        const fin = input.selectionEnd ?? input.value.length;

        const nuevoValor =
            input.value.substring(0, inicio) +
            "." +
            input.value.substring(fin);

        input.value = normalizarDecimalTexto(nuevoValor);

        const nuevaPosicion = inicio + 1;
        input.setSelectionRange(nuevaPosicion, nuevaPosicion);
    }
});

document.addEventListener("input", function (e) {
    const input = e.target;

    if (!(input instanceof HTMLInputElement) && !(input instanceof HTMLTextAreaElement)) {
        return;
    }

    if (input.dataset.soloNumeros === "true") {
        input.value = soloDigitos(input.value);
    }

    if (input.dataset.formatoNit === "true") {
        input.value = formatearNit(input.value);
    }

    if (input.dataset.formatoTelefono === "true") {
        input.value = formatearTelefono(input.value);
    }

    if (input.dataset.soloLetras === "true") {
        input.value = input.value.replace(/[^a-zA-ZáéíóúÁÉÍÓÚñÑ\s]/g, "");
    }

    if (input.dataset.alfanumericoCodigo === "true") {
        input.value = input.value.replace(/[^a-zA-Z0-9\-_]/g, "").toUpperCase();
    }

    if (input.dataset.decimalPositivo === "true") {
        /*
           Importante:
           Si el input es type="number", no se modifica en cada tecla,
           porque algunos navegadores limpian el campo al escribir punto o coma.
           Se normaliza hasta blur/submit.
        */
        if (input.type !== "number") {
            input.value = normalizarDecimalTexto(input.value);
        }
    }

    if (input.dataset.referencia === "true") {
        input.value = input.value.replace(/[^a-zA-Z0-9\-_\/\s]/g, "").toUpperCase();
    }
});

document.addEventListener("blur", function (e) {
    const input = e.target;

    if (!(input instanceof HTMLInputElement)) {
        return;
    }

    if (input.dataset.decimalPositivo === "true" && input.value) {
        input.value = normalizarDecimalFinal(input.value);
    }
}, true);

document.addEventListener("submit", function (e) {
    const form = e.target;

    if (!(form instanceof HTMLFormElement)) {
        return;
    }

    const decimales = form.querySelectorAll("[data-decimal-positivo='true']");

    decimales.forEach(function (input) {
        if (input.value) {
            input.value = normalizarDecimalFinal(input.value);
        }
    });

    const fechaInputs = form.querySelectorAll("input[type='date'][data-no-futuro='true']");
    const hoy = new Date();
    hoy.setHours(0, 0, 0, 0);

    for (const input of fechaInputs) {
        if (!input.value) continue;

        const fecha = new Date(input.value + "T00:00:00");

        if (fecha > hoy) {
            e.preventDefault();
            alert("La fecha no puede ser mayor a la fecha actual.");
            input.focus();
            return;
        }
    }
});