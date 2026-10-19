document.querySelectorAll('[data-copy-log]').forEach(button => {
    button.addEventListener('click', async () => {
        const log = document.getElementById(button.dataset.copyLog);
        const status = button.parentElement.querySelector('[data-copy-status]');
        const messages = document.getElementById('history-copy-messages');
        if (!log || !status) {
            return;
        }

        button.disabled = true;
        status.textContent = '';
        try {
            await navigator.clipboard.writeText(log.textContent);
            status.textContent = messages?.querySelector('[data-key="copied"]')?.textContent || 'Log copied.';
        } catch {
            status.textContent = messages?.querySelector('[data-key="failed"]')?.textContent || 'Copy failed. Select the log text and copy it manually.';
        } finally {
            button.disabled = false;
        }
    });
});
