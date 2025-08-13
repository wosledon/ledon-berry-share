// 图表管理工具
window.chartInstances = {};

// 创建或更新图表
window.createChart = (canvasId, type, data, options = {}) => {
    try {
        const canvas = document.getElementById(canvasId);
        if (!canvas) {
            console.error(`Canvas element with id '${canvasId}' not found`);
            return false;
        }

        // 如果已存在图表，先销毁
        if (window.chartInstances[canvasId]) {
            window.chartInstances[canvasId].destroy();
        }

        const ctx = canvas.getContext('2d');
        
        // 默认配置
        const defaultOptions = {
            responsive: true,
            maintainAspectRatio: false,
            plugins: {
                legend: {
                    position: 'top',
                }
            }
        };

        // 合并配置
        const finalOptions = {
            ...defaultOptions,
            ...options
        };

        // 创建新图表
        window.chartInstances[canvasId] = new Chart(ctx, {
            type: type,
            data: data,
            options: finalOptions
        });

        return true;
    } catch (error) {
        console.error('创建图表失败:', error);
        return false;
    }
};

// 销毁图表
window.destroyChart = (canvasId) => {
    if (window.chartInstances[canvasId]) {
        window.chartInstances[canvasId].destroy();
        delete window.chartInstances[canvasId];
    }
};

// 销毁所有图表
window.destroyAllCharts = () => {
    Object.keys(window.chartInstances).forEach(canvasId => {
        window.destroyChart(canvasId);
    });
};

// 预定义颜色方案
window.getColorScheme = (count) => {
    const colors = [
        '#FF6384', '#36A2EB', '#FFCE56', '#4BC0C0', '#9966FF', '#FF9F40',
        '#FF6B6B', '#4ECDC4', '#45B7D1', '#96CEB4', '#FFEAA7', '#DDA0DD',
        '#98D8C8', '#F7DC6F', '#BB8FCE', '#85C1E9', '#F8C471', '#82E0AA'
    ];
    return colors.slice(0, count);
};

// 趋势图表配置
window.createTrendChart = (canvasId, labels, datasets) => {
    const data = {
        labels: labels,
        datasets: datasets.map((dataset, index) => ({
            ...dataset,
            borderColor: dataset.borderColor || window.getColorScheme(datasets.length)[index],
            backgroundColor: dataset.backgroundColor || window.getColorScheme(datasets.length)[index] + '20',
            tension: 0.4,
            fill: false
        }))
    };

    const options = {
        scales: {
            y: {
                beginAtZero: true,
                ticks: {
                    callback: function(value) {
                        return '¥' + value.toLocaleString();
                    }
                }
            }
        },
        plugins: {
            title: {
                display: true,
                text: '流水趋势图'
            }
        }
    };

    return window.createChart(canvasId, 'line', data, options);
};

// 饼图配置
window.createPieChart = (canvasId, labels, data, title) => {
    const chartData = {
        labels: labels,
        datasets: [{
            data: data,
            backgroundColor: window.getColorScheme(labels.length),
            borderWidth: 1
        }]
    };

    const options = {
        plugins: {
            title: {
                display: true,
                text: title || '饼图'
            },
            legend: {
                position: 'right'
            },
            tooltip: {
                callbacks: {
                    label: function(context) {
                        const total = context.dataset.data.reduce((a, b) => a + b, 0);
                        const percentage = ((context.parsed / total) * 100).toFixed(1);
                        return `${context.label}: ¥${context.parsed.toLocaleString()} (${percentage}%)`;
                    }
                }
            }
        }
    };

    return window.createChart(canvasId, 'pie', chartData, options);
};

// 环形图配置
window.createDoughnutChart = (canvasId, labels, data, title) => {
    const chartData = {
        labels: labels,
        datasets: [{
            data: data,
            backgroundColor: window.getColorScheme(labels.length),
            borderWidth: 2
        }]
    };

    const options = {
        plugins: {
            title: {
                display: true,
                text: title || '环形图'
            },
            legend: {
                position: 'bottom'
            },
            tooltip: {
                callbacks: {
                    label: function(context) {
                        const total = context.dataset.data.reduce((a, b) => a + b, 0);
                        const percentage = ((context.parsed / total) * 100).toFixed(1);
                        return `${context.label}: ¥${context.parsed.toLocaleString()} (${percentage}%)`;
                    }
                }
            }
        },
        cutout: '50%'
    };

    return window.createChart(canvasId, 'doughnut', chartData, options);
};

// 柱状图配置
window.createBarChart = (canvasId, labels, datasets, horizontal = false) => {
    const data = {
        labels: labels,
        datasets: datasets.map((dataset, index) => ({
            ...dataset,
            backgroundColor: dataset.backgroundColor || window.getColorScheme(datasets.length)[index] + '80',
            borderColor: dataset.borderColor || window.getColorScheme(datasets.length)[index],
            borderWidth: 1
        }))
    };

    const options = {
        plugins: {
            title: {
                display: true,
                text: horizontal ? '水平柱状图' : '柱状图'
            }
        },
        scales: {
            y: {
                beginAtZero: true,
                ticks: horizontal ? {} : {
                    callback: function(value) {
                        return '¥' + value.toLocaleString();
                    }
                }
            }
        }
    };

    if (horizontal) {
        options.indexAxis = 'y';
    }

    return window.createChart(canvasId, 'bar', data, options);
};

// 饼图配置
window.createPieChart = (canvasId, labels, dataValues, title = '') => {
    const data = {
        labels: labels,
        datasets: [{
            data: dataValues,
            backgroundColor: window.getColorScheme(labels.length),
            borderWidth: 2,
            borderColor: '#fff'
        }]
    };

    const options = {
        plugins: {
            title: {
                display: !!title,
                text: title
            },
            tooltip: {
                callbacks: {
                    label: function(context) {
                        const total = context.dataset.data.reduce((a, b) => a + b, 0);
                        const percentage = ((context.parsed / total) * 100).toFixed(1);
                        return context.label + ': ¥' + context.parsed.toLocaleString() + ' (' + percentage + '%)';
                    }
                }
            }
        }
    };

    return window.createChart(canvasId, 'pie', data, options);
};

// 柱状图配置
window.createBarChart = (canvasId, labels, datasets, horizontal = false) => {
    const data = {
        labels: labels,
        datasets: datasets.map((dataset, index) => ({
            ...dataset,
            backgroundColor: dataset.backgroundColor || window.getColorScheme(datasets.length)[index] + '80',
            borderColor: dataset.borderColor || window.getColorScheme(datasets.length)[index],
            borderWidth: 1
        }))
    };

    const options = {
        indexAxis: horizontal ? 'y' : 'x',
        scales: {
            [horizontal ? 'x' : 'y']: {
                beginAtZero: true,
                ticks: {
                    callback: function(value) {
                        return '¥' + value.toLocaleString();
                    }
                }
            }
        },
        plugins: {
            tooltip: {
                callbacks: {
                    label: function(context) {
                        return context.dataset.label + ': ¥' + context.parsed[horizontal ? 'x' : 'y'].toLocaleString();
                    }
                }
            }
        }
    };

    return window.createChart(canvasId, 'bar', data, options);
};

// 环形图配置
window.createDoughnutChart = (canvasId, labels, dataValues, title = '') => {
    const data = {
        labels: labels,
        datasets: [{
            data: dataValues,
            backgroundColor: window.getColorScheme(labels.length),
            borderWidth: 2,
            borderColor: '#fff'
        }]
    };

    const options = {
        plugins: {
            title: {
                display: !!title,
                text: title
            },
            tooltip: {
                callbacks: {
                    label: function(context) {
                        const total = context.dataset.data.reduce((a, b) => a + b, 0);
                        const percentage = ((context.parsed / total) * 100).toFixed(1);
                        return context.label + ': ¥' + context.parsed.toLocaleString() + ' (' + percentage + '%)';
                    }
                }
            }
        }
    };

    return window.createChart(canvasId, 'doughnut', data, options);
};
