document.addEventListener("input", function (e) {
    const input = e.target;

    if (!(input instanceof HTMLInputElement) && !(input instanceof HTMLTextAreaElement)) {
        return;
    }

    if (input.dataset.soloNumeros === "true") {
        input.value = input.value.replace(/\D/g, "");
    }

    if (input.dataset.soloLetras === "true") {
        input.value = input.value.replace(/[^a-zA-ZáéíóúÁÉÍÓÚñÑ\s]/g, "");
    }

    if (input.dataset.alfanumericoCodigo === "true") {
        input.value = input.value.replace(/[^a-zA-Z0-9\-_]/g, "").toUpperCase();
    }

    if (input.dataset.decimalPositivo === "true") {
        input.value = input.value
            .replace(/[^0-9.]/g, "")
            .replace(/(\..*)\./g, "$1");
    }

    if (input.dataset.referencia === "true") {
        input.value = input.value.replace(/[^a-zA-Z0-9\-_\/\s]/g, "").toUpperCase();
    }
});

document.addEventListener("submit", function (e) {
    const form = e.target;

    if (!(form instanceof HTMLFormElement)) {
        return;
    }

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