(function () {
    const script = document.currentScript;
    const overlay = document.getElementById('doctor-message-overlay');
    const dialog = overlay?.querySelector('.doctor-message-dialog');
    const title = document.getElementById('doctor-message-title');
    const message = document.getElementById('doctor-message-text');
    const signature = document.getElementById('doctor-message-signature');
    const closeButton = document.getElementById('doctor-message-close');
    let doctorNamePromise;
    let closeMessage;
    let previousFocus;

    async function getDoctorName() {
        if (!doctorNamePromise) {
            doctorNamePromise = fetch(script.dataset.doctorAccountUrl, {
                credentials: 'same-origin',
                headers: { Accept: 'application/json' }
            })
                .then(response => response.ok ? response.json() : null)
                .then(doctor => {
                    if (!doctor) return '';
                    const name = doctor.name ?? doctor.Name ?? '';
                    const surname = doctor.surname ?? doctor.Surname ?? '';
                    const fullName = `${name} ${surname}`.trim();
                    return fullName ? `Dr. ${fullName}` : '';
                })
                .catch(() => '');
        }
        return doctorNamePromise;
    }

    window.showDoctorMessage = async function ({ heading = 'Notification', text = '', doctor = true } = {}) {
        if (!overlay || !dialog) return;
        title.textContent = heading;
        message.textContent = text;
        const doctorDisplayName = doctor ? await getDoctorName() : '';
        signature.textContent = doctorDisplayName ? `Recorded by ${doctorDisplayName}` : '';
        signature.hidden = !signature.textContent;
        previousFocus = document.activeElement;
        overlay.hidden = false;
        closeButton.focus();

        return new Promise(resolve => {
            closeMessage = () => {
                overlay.hidden = true;
                closeButton.removeEventListener('click', closeMessage);
                document.removeEventListener('keydown', onKeyDown);
                previousFocus?.focus?.();
                resolve();
            };
            closeButton.addEventListener('click', closeMessage, { once: true });
            document.addEventListener('keydown', onKeyDown);
        });
    };

    function onKeyDown(event) {
        if (event.key === 'Escape' && closeMessage) closeMessage();
    }
})();
