// TaxiCompare – Chart.js interop helpers

window.renderPriceChart = function (canvasId, labels, datasets) {
    const canvas = document.getElementById(canvasId);
    if (!canvas) return;

    // Destroy existing chart if any
    if (canvas._chartInstance) {
        canvas._chartInstance.destroy();
    }

    const ctx = canvas.getContext('2d');
    canvas._chartInstance = new Chart(ctx, {
        type: 'line',
        data: { labels, datasets },
        options: {
            responsive: true,
            maintainAspectRatio: false,
            interaction: { mode: 'index', intersect: false },
            plugins: {
                legend: {
                    labels: { color: 'rgba(240,244,255,0.6)', font: { family: 'Inter', size: 12 }, boxWidth: 12, boxHeight: 12 }
                },
                tooltip: {
                    backgroundColor: 'rgba(10,15,28,0.95)',
                    borderColor: 'rgba(255,255,255,0.1)',
                    borderWidth: 1,
                    titleColor: '#F0F4FF',
                    bodyColor: 'rgba(240,244,255,0.7)',
                    padding: 12,
                    callbacks: {
                        label: ctx => ` ${ctx.dataset.label}: ${ctx.parsed.y.toFixed(0)} ₽`
                    }
                }
            },
            scales: {
                x: {
                    grid: { color: 'rgba(255,255,255,0.04)' },
                    ticks: { color: 'rgba(240,244,255,0.4)', font: { size: 11 } }
                },
                y: {
                    grid: { color: 'rgba(255,255,255,0.04)' },
                    ticks: {
                        color: 'rgba(240,244,255,0.4)', font: { size: 11 },
                        callback: v => `${v.toFixed(0)} ₽`
                    }
                }
            }
        }
    });
};

// Destroy a chart by canvas ID
window.destroyChart = function (canvasId) {
    const canvas = document.getElementById(canvasId);
    if (canvas && canvas._chartInstance) {
        canvas._chartInstance.destroy();
        canvas._chartInstance = null;
    }
};
