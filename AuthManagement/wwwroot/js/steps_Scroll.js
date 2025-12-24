window.scrollToActiveStep = () => {
    const header = document.getElementById("stepperHeader");
    if (!header) return;

    const activeStep = header.querySelector(".step.active");
    if (!activeStep) return;

    activeStep.scrollIntoView({
        behavior: "smooth",
        inline: "center",
        block: "nearest"
    });
};