// Please see documentation at https://learn.microsoft.com/aspnet/core/client-side/bundling-and-minification
// for details on configuring this project to bundle and minify static web assets.

// Write your JavaScript code.

(function () {
    const table = document.querySelector("[data-admin-patients-table]");
    if (!table) {
        return;
    }

    const rows = Array.from(table.querySelectorAll("[data-patient-row]"));
    const emptyRow = document.getElementById("patientsEmptyRow");
    const searchInput = document.getElementById("patientSearch");
    const doctorFilter = document.getElementById("doctorFilter");
    const pageSizeSelect = document.getElementById("patientPageSize");
    const resultCount = document.getElementById("patientResultCount");
    const currentPageLabel = document.getElementById("patientCurrentPage");
    const paginationButtons = Array.from(document.querySelectorAll("[data-patient-page]"));
    let currentPage = 1;

    function normalize(value) {
        return (value || "").toString().trim().toLowerCase();
    }

    function getFilteredRows() {
        const query = normalize(searchInput && searchInput.value);
        const selectedDoctor = doctorFilter ? doctorFilter.value : "";

        return rows.filter(function (row) {
            const matchesSearch = !query || normalize(row.dataset.search).indexOf(query) !== -1;
            const matchesDoctor = !selectedDoctor || row.dataset.doctor === selectedDoctor;

            return matchesSearch && matchesDoctor;
        });
    }

    function renderPatients() {
        const pageSize = parseInt(pageSizeSelect && pageSizeSelect.value ? pageSizeSelect.value : "10", 10);
        const filteredRows = getFilteredRows();
        const totalPages = Math.max(1, Math.ceil(filteredRows.length / pageSize));

        if (currentPage > totalPages) {
            currentPage = totalPages;
        }

        const start = (currentPage - 1) * pageSize;
        const visibleRows = filteredRows.slice(start, start + pageSize);

        rows.forEach(function (row) {
            row.hidden = true;
        });

        visibleRows.forEach(function (row) {
            row.hidden = false;
        });

        if (emptyRow) {
            emptyRow.hidden = filteredRows.length > 0;
        }

        if (resultCount) {
            resultCount.textContent = filteredRows.length.toString();
        }

        if (currentPageLabel) {
            currentPageLabel.textContent = currentPage.toString();
        }

        paginationButtons.forEach(function (button) {
            const action = button.dataset.patientPage;
            button.disabled =
                (action === "first" || action === "prev") && currentPage <= 1 ||
                (action === "next" || action === "last") && currentPage >= totalPages ||
                filteredRows.length === 0;
        });
    }

    function resetAndRender() {
        currentPage = 1;
        renderPatients();
    }

    if (searchInput) {
        searchInput.addEventListener("input", resetAndRender);
    }

    if (doctorFilter) {
        doctorFilter.addEventListener("change", resetAndRender);
    }

    if (pageSizeSelect) {
        pageSizeSelect.addEventListener("change", resetAndRender);
    }

    paginationButtons.forEach(function (button) {
        button.addEventListener("click", function () {
            const pageSize = parseInt(pageSizeSelect && pageSizeSelect.value ? pageSizeSelect.value : "10", 10);
            const totalPages = Math.max(1, Math.ceil(getFilteredRows().length / pageSize));

            if (button.dataset.patientPage === "first") {
                currentPage = 1;
            } else if (button.dataset.patientPage === "prev") {
                currentPage = Math.max(1, currentPage - 1);
            } else if (button.dataset.patientPage === "next") {
                currentPage = Math.min(totalPages, currentPage + 1);
            } else if (button.dataset.patientPage === "last") {
                currentPage = totalPages;
            }

            renderPatients();
        });
    });

    renderPatients();
})();

(function () {
    const table = document.querySelector("[data-admin-doctors-table]");
    if (!table) {
        return;
    }

    const rows = Array.from(table.querySelectorAll("[data-doctor-row]"));
    const emptyRow = document.getElementById("doctorsEmptyRow");
    const searchInput = document.getElementById("doctorSearch");
    const specializationFilter = document.getElementById("specializationFilter");
    const pageSizeSelect = document.getElementById("doctorPageSize");
    const resultCount = document.getElementById("doctorResultCount");
    const currentPageLabel = document.getElementById("doctorCurrentPage");
    const paginationButtons = Array.from(document.querySelectorAll("[data-doctor-page]"));
    let currentPage = 1;

    function normalize(value) {
        return (value || "").toString().trim().toLowerCase();
    }

    function getFilteredRows() {
        const query = normalize(searchInput && searchInput.value);
        const selectedSpecialization = specializationFilter ? specializationFilter.value : "";

        return rows.filter(function (row) {
            const matchesSearch = !query || normalize(row.dataset.search).indexOf(query) !== -1;
            const matchesSpecialization = !selectedSpecialization || row.dataset.specialization === selectedSpecialization;

            return matchesSearch && matchesSpecialization;
        });
    }

    function renderDoctors() {
        const pageSize = parseInt(pageSizeSelect && pageSizeSelect.value ? pageSizeSelect.value : "10", 10);
        const filteredRows = getFilteredRows();
        const totalPages = Math.max(1, Math.ceil(filteredRows.length / pageSize));

        if (currentPage > totalPages) {
            currentPage = totalPages;
        }

        const start = (currentPage - 1) * pageSize;
        const visibleRows = filteredRows.slice(start, start + pageSize);

        rows.forEach(function (row) {
            row.hidden = true;
        });

        visibleRows.forEach(function (row) {
            row.hidden = false;
        });

        if (emptyRow) {
            emptyRow.hidden = filteredRows.length > 0;
        }

        if (resultCount) {
            resultCount.textContent = filteredRows.length.toString();
        }

        if (currentPageLabel) {
            currentPageLabel.textContent = currentPage.toString();
        }

        paginationButtons.forEach(function (button) {
            const action = button.dataset.doctorPage;
            button.disabled =
                (action === "first" || action === "prev") && currentPage <= 1 ||
                (action === "next" || action === "last") && currentPage >= totalPages ||
                filteredRows.length === 0;
        });
    }

    function resetAndRender() {
        currentPage = 1;
        renderDoctors();
    }

    if (searchInput) {
        searchInput.addEventListener("input", resetAndRender);
    }

    if (specializationFilter) {
        specializationFilter.addEventListener("change", resetAndRender);
    }

    if (pageSizeSelect) {
        pageSizeSelect.addEventListener("change", resetAndRender);
    }

    paginationButtons.forEach(function (button) {
        button.addEventListener("click", function () {
            const pageSize = parseInt(pageSizeSelect && pageSizeSelect.value ? pageSizeSelect.value : "10", 10);
            const totalPages = Math.max(1, Math.ceil(getFilteredRows().length / pageSize));

            if (button.dataset.doctorPage === "first") {
                currentPage = 1;
            } else if (button.dataset.doctorPage === "prev") {
                currentPage = Math.max(1, currentPage - 1);
            } else if (button.dataset.doctorPage === "next") {
                currentPage = Math.min(totalPages, currentPage + 1);
            } else if (button.dataset.doctorPage === "last") {
                currentPage = totalPages;
            }

            renderDoctors();
        });
    });

    renderDoctors();
})();

(function () {
    const table = document.querySelector("[data-admin-medicines-table]");
    if (!table) {
        return;
    }

    const rows = Array.from(table.querySelectorAll("[data-medicine-row]"));
    const emptyRow = document.getElementById("medicinesEmptyRow");
    const searchInput = document.getElementById("medicineSearch");
    const pageSizeSelect = document.getElementById("medicinePageSize");
    const resultCount = document.getElementById("medicineResultCount");
    const currentPageLabel = document.getElementById("medicineCurrentPage");
    const paginationButtons = Array.from(document.querySelectorAll("[data-medicine-page]"));
    let currentPage = 1;

    function normalize(value) {
        return (value || "").toString().trim().toLowerCase();
    }

    function getFilteredRows() {
        const query = normalize(searchInput && searchInput.value);

        return rows.filter(function (row) {
            return !query || normalize(row.dataset.search).indexOf(query) !== -1;
        });
    }

    function renderMedicines() {
        const pageSize = parseInt(pageSizeSelect && pageSizeSelect.value ? pageSizeSelect.value : "10", 10);
        const filteredRows = getFilteredRows();
        const totalPages = Math.max(1, Math.ceil(filteredRows.length / pageSize));

        if (currentPage > totalPages) {
            currentPage = totalPages;
        }

        const start = (currentPage - 1) * pageSize;
        const visibleRows = filteredRows.slice(start, start + pageSize);

        rows.forEach(function (row) {
            row.hidden = true;
        });

        visibleRows.forEach(function (row) {
            row.hidden = false;
        });

        if (emptyRow) {
            emptyRow.hidden = filteredRows.length > 0;
        }

        if (resultCount) {
            resultCount.textContent = filteredRows.length.toString();
        }

        if (currentPageLabel) {
            currentPageLabel.textContent = currentPage.toString();
        }

        paginationButtons.forEach(function (button) {
            const action = button.dataset.medicinePage;
            button.disabled =
                (action === "first" || action === "prev") && currentPage <= 1 ||
                (action === "next" || action === "last") && currentPage >= totalPages ||
                filteredRows.length === 0;
        });
    }

    function resetAndRender() {
        currentPage = 1;
        renderMedicines();
    }

    if (searchInput) {
        searchInput.addEventListener("input", resetAndRender);
    }

    if (pageSizeSelect) {
        pageSizeSelect.addEventListener("change", resetAndRender);
    }

    paginationButtons.forEach(function (button) {
        button.addEventListener("click", function () {
            const pageSize = parseInt(pageSizeSelect && pageSizeSelect.value ? pageSizeSelect.value : "10", 10);
            const totalPages = Math.max(1, Math.ceil(getFilteredRows().length / pageSize));

            if (button.dataset.medicinePage === "first") {
                currentPage = 1;
            } else if (button.dataset.medicinePage === "prev") {
                currentPage = Math.max(1, currentPage - 1);
            } else if (button.dataset.medicinePage === "next") {
                currentPage = Math.min(totalPages, currentPage + 1);
            } else if (button.dataset.medicinePage === "last") {
                currentPage = totalPages;
            }

            renderMedicines();
        });
    });

    renderMedicines();
})();

(function () {
    const table = document.querySelector("[data-admin-account-requests-table]");
    if (!table) {
        return;
    }

    const rows = Array.from(table.querySelectorAll("[data-account-request-row]"));
    const emptyRow = document.getElementById("accountRequestsEmptyRow");
    const searchInput = document.getElementById("accountRequestSearch");
    const pageSizeSelect = document.getElementById("accountRequestPageSize");
    const resultCount = document.getElementById("accountRequestResultCount");
    const currentPageLabel = document.getElementById("accountRequestCurrentPage");
    const paginationButtons = Array.from(document.querySelectorAll("[data-account-request-page]"));
    let currentPage = 1;

    function normalize(value) {
        return (value || "").toString().trim().toLowerCase();
    }

    function getFilteredRows() {
        const query = normalize(searchInput && searchInput.value);

        return rows.filter(function (row) {
            return !query || normalize(row.dataset.search).indexOf(query) !== -1;
        });
    }

    function renderAccountRequests() {
        const pageSize = parseInt(pageSizeSelect && pageSizeSelect.value ? pageSizeSelect.value : "10", 10);
        const filteredRows = getFilteredRows();
        const totalPages = Math.max(1, Math.ceil(filteredRows.length / pageSize));

        if (currentPage > totalPages) {
            currentPage = totalPages;
        }

        const start = (currentPage - 1) * pageSize;
        const visibleRows = filteredRows.slice(start, start + pageSize);

        rows.forEach(function (row) {
            row.hidden = true;
        });

        visibleRows.forEach(function (row) {
            row.hidden = false;
        });

        if (emptyRow) {
            emptyRow.hidden = filteredRows.length > 0;
        }

        if (resultCount) {
            resultCount.textContent = filteredRows.length.toString();
        }

        if (currentPageLabel) {
            currentPageLabel.textContent = currentPage.toString();
        }

        paginationButtons.forEach(function (button) {
            const action = button.dataset.accountRequestPage;
            button.disabled =
                (action === "first" || action === "prev") && currentPage <= 1 ||
                (action === "next" || action === "last") && currentPage >= totalPages ||
                filteredRows.length === 0;
        });
    }

    function resetAndRender() {
        currentPage = 1;
        renderAccountRequests();
    }

    if (searchInput) {
        searchInput.addEventListener("input", resetAndRender);
    }

    if (pageSizeSelect) {
        pageSizeSelect.addEventListener("change", resetAndRender);
    }

    paginationButtons.forEach(function (button) {
        button.addEventListener("click", function () {
            const pageSize = parseInt(pageSizeSelect && pageSizeSelect.value ? pageSizeSelect.value : "10", 10);
            const totalPages = Math.max(1, Math.ceil(getFilteredRows().length / pageSize));

            if (button.dataset.accountRequestPage === "first") {
                currentPage = 1;
            } else if (button.dataset.accountRequestPage === "prev") {
                currentPage = Math.max(1, currentPage - 1);
            } else if (button.dataset.accountRequestPage === "next") {
                currentPage = Math.min(totalPages, currentPage + 1);
            } else if (button.dataset.accountRequestPage === "last") {
                currentPage = totalPages;
            }

            renderAccountRequests();
        });
    });

    renderAccountRequests();
})();

(function () {
    const table = document.querySelector("[data-doctor-patients-table]");
    if (!table) {
        return;
    }

    const rows = Array.from(table.querySelectorAll("[data-doctor-patient-row]"));
    const emptyRow = document.getElementById("doctorPatientsEmptyRow");
    const searchInput = document.getElementById("doctorPatientSearch");
    const pageSizeSelect = document.getElementById("doctorPatientPageSize");
    const resultCount = document.getElementById("doctorPatientResultCount");
    const currentPageLabel = document.getElementById("doctorPatientCurrentPage");
    const paginationButtons = Array.from(document.querySelectorAll("[data-doctor-patient-page]"));
    let currentPage = 1;

    function normalize(value) {
        return (value || "").toString().trim().toLowerCase();
    }

    function getFilteredRows() {
        const query = normalize(searchInput && searchInput.value);

        return rows.filter(function (row) {
            return !query || normalize(row.dataset.search).indexOf(query) !== -1;
        });
    }

    function renderDoctorPatients() {
        const pageSize = parseInt(pageSizeSelect && pageSizeSelect.value ? pageSizeSelect.value : "10", 10);
        const filteredRows = getFilteredRows();
        const totalPages = Math.max(1, Math.ceil(filteredRows.length / pageSize));

        if (currentPage > totalPages) {
            currentPage = totalPages;
        }

        const start = (currentPage - 1) * pageSize;
        const visibleRows = filteredRows.slice(start, start + pageSize);

        rows.forEach(function (row) {
            row.hidden = true;
        });

        visibleRows.forEach(function (row) {
            row.hidden = false;
        });

        if (emptyRow) {
            emptyRow.hidden = filteredRows.length > 0;
        }

        if (resultCount) {
            resultCount.textContent = filteredRows.length.toString();
        }

        if (currentPageLabel) {
            currentPageLabel.textContent = currentPage.toString();
        }

        paginationButtons.forEach(function (button) {
            const action = button.dataset.doctorPatientPage;
            button.disabled =
                (action === "first" || action === "prev") && currentPage <= 1 ||
                (action === "next" || action === "last") && currentPage >= totalPages ||
                filteredRows.length === 0;
        });
    }

    function resetAndRender() {
        currentPage = 1;
        renderDoctorPatients();
    }

    if (searchInput) {
        searchInput.addEventListener("input", resetAndRender);
    }

    if (pageSizeSelect) {
        pageSizeSelect.addEventListener("change", resetAndRender);
    }

    paginationButtons.forEach(function (button) {
        button.addEventListener("click", function () {
            const pageSize = parseInt(pageSizeSelect && pageSizeSelect.value ? pageSizeSelect.value : "10", 10);
            const totalPages = Math.max(1, Math.ceil(getFilteredRows().length / pageSize));

            if (button.dataset.doctorPatientPage === "first") {
                currentPage = 1;
            } else if (button.dataset.doctorPatientPage === "prev") {
                currentPage = Math.max(1, currentPage - 1);
            } else if (button.dataset.doctorPatientPage === "next") {
                currentPage = Math.min(totalPages, currentPage + 1);
            } else if (button.dataset.doctorPatientPage === "last") {
                currentPage = totalPages;
            }

            renderDoctorPatients();
        });
    });

    renderDoctorPatients();
})();

(function () {
    const table = document.querySelector("[data-doctor-requests-table]");
    if (!table) {
        return;
    }

    const rows = Array.from(table.querySelectorAll("[data-doctor-request-row]"));
    const emptyRow = document.getElementById("doctorRequestsEmptyRow");
    const searchInput = document.getElementById("doctorRequestSearch");
    const pageSizeSelect = document.getElementById("doctorRequestPageSize");
    const resultCount = document.getElementById("doctorRequestResultCount");
    const currentPageLabel = document.getElementById("doctorRequestCurrentPage");
    const paginationButtons = Array.from(document.querySelectorAll("[data-doctor-request-page]"));
    let currentPage = 1;

    function normalize(value) {
        return (value || "").toString().trim().toLowerCase();
    }

    function getFilteredRows() {
        const query = normalize(searchInput && searchInput.value);

        return rows.filter(function (row) {
            return !query || normalize(row.dataset.search).indexOf(query) !== -1;
        });
    }

    function renderDoctorRequests() {
        const pageSize = parseInt(pageSizeSelect && pageSizeSelect.value ? pageSizeSelect.value : "10", 10);
        const filteredRows = getFilteredRows();
        const totalPages = Math.max(1, Math.ceil(filteredRows.length / pageSize));

        if (currentPage > totalPages) {
            currentPage = totalPages;
        }

        const start = (currentPage - 1) * pageSize;
        const visibleRows = filteredRows.slice(start, start + pageSize);

        rows.forEach(function (row) {
            row.hidden = true;
        });

        visibleRows.forEach(function (row) {
            row.hidden = false;
        });

        if (emptyRow) {
            emptyRow.hidden = filteredRows.length > 0;
        }

        if (resultCount) {
            resultCount.textContent = filteredRows.length.toString();
        }

        if (currentPageLabel) {
            currentPageLabel.textContent = currentPage.toString();
        }

        paginationButtons.forEach(function (button) {
            const action = button.dataset.doctorRequestPage;
            button.disabled =
                (action === "first" || action === "prev") && currentPage <= 1 ||
                (action === "next" || action === "last") && currentPage >= totalPages ||
                filteredRows.length === 0;
        });
    }

    function resetAndRender() {
        currentPage = 1;
        renderDoctorRequests();
    }

    if (searchInput) {
        searchInput.addEventListener("input", resetAndRender);
    }

    if (pageSizeSelect) {
        pageSizeSelect.addEventListener("change", resetAndRender);
    }

    paginationButtons.forEach(function (button) {
        button.addEventListener("click", function () {
            const pageSize = parseInt(pageSizeSelect && pageSizeSelect.value ? pageSizeSelect.value : "10", 10);
            const totalPages = Math.max(1, Math.ceil(getFilteredRows().length / pageSize));

            if (button.dataset.doctorRequestPage === "first") {
                currentPage = 1;
            } else if (button.dataset.doctorRequestPage === "prev") {
                currentPage = Math.max(1, currentPage - 1);
            } else if (button.dataset.doctorRequestPage === "next") {
                currentPage = Math.min(totalPages, currentPage + 1);
            } else if (button.dataset.doctorRequestPage === "last") {
                currentPage = totalPages;
            }

            renderDoctorRequests();
        });
    });

    renderDoctorRequests();
})();

(function () {
    const table = document.querySelector("[data-side-effects-table]");
    if (!table) {
        return;
    }

    const rows = Array.from(table.querySelectorAll("[data-side-effect-row]"));
    const emptyRow = document.getElementById("sideEffectsEmptyRow");
    const searchInput = document.getElementById("sideEffectSearch");
    const pageSizeSelect = document.getElementById("sideEffectPageSize");
    const resultCount = document.getElementById("sideEffectResultCount");
    const currentPageLabel = document.getElementById("sideEffectCurrentPage");
    const paginationButtons = Array.from(document.querySelectorAll("[data-side-effect-page]"));
    let currentPage = 1;

    function normalize(value) {
        return (value || "").toString().trim().toLowerCase();
    }

    function getFilteredRows() {
        const query = normalize(searchInput && searchInput.value);

        return rows.filter(function (row) {
            return !query || normalize(row.dataset.search).indexOf(query) !== -1;
        });
    }

    function renderSideEffects() {
        const pageSize = parseInt(pageSizeSelect && pageSizeSelect.value ? pageSizeSelect.value : "10", 10);
        const filteredRows = getFilteredRows();
        const totalPages = Math.max(1, Math.ceil(filteredRows.length / pageSize));

        if (currentPage > totalPages) {
            currentPage = totalPages;
        }

        const start = (currentPage - 1) * pageSize;
        const visibleRows = filteredRows.slice(start, start + pageSize);

        rows.forEach(function (row) {
            row.hidden = true;
        });

        visibleRows.forEach(function (row) {
            row.hidden = false;
        });

        if (emptyRow) {
            emptyRow.hidden = filteredRows.length > 0;
        }

        if (resultCount) {
            resultCount.textContent = filteredRows.length.toString();
        }

        if (currentPageLabel) {
            currentPageLabel.textContent = currentPage.toString();
        }

        paginationButtons.forEach(function (button) {
            const action = button.dataset.sideEffectPage;
            button.disabled =
                (action === "first" || action === "prev") && currentPage <= 1 ||
                (action === "next" || action === "last") && currentPage >= totalPages ||
                filteredRows.length === 0;
        });
    }

    function resetAndRender() {
        currentPage = 1;
        renderSideEffects();
    }

    if (searchInput) {
        searchInput.addEventListener("input", resetAndRender);
    }

    if (pageSizeSelect) {
        pageSizeSelect.addEventListener("change", resetAndRender);
    }

    paginationButtons.forEach(function (button) {
        button.addEventListener("click", function () {
            const pageSize = parseInt(pageSizeSelect && pageSizeSelect.value ? pageSizeSelect.value : "10", 10);
            const totalPages = Math.max(1, Math.ceil(getFilteredRows().length / pageSize));

            if (button.dataset.sideEffectPage === "first") {
                currentPage = 1;
            } else if (button.dataset.sideEffectPage === "prev") {
                currentPage = Math.max(1, currentPage - 1);
            } else if (button.dataset.sideEffectPage === "next") {
                currentPage = Math.min(totalPages, currentPage + 1);
            } else if (button.dataset.sideEffectPage === "last") {
                currentPage = totalPages;
            }

            renderSideEffects();
        });
    });

    renderSideEffects();
})();

(function () {
    const list = document.querySelector("[data-doctor-side-effects-list]");
    if (!list) {
        return;
    }

    const cards = Array.from(list.querySelectorAll("[data-doctor-side-effect-card]"));
    const emptyRow = document.getElementById("doctorSideEffectsEmptyRow");
    const searchInput = document.getElementById("doctorSideEffectSearch");
    const pageSizeSelect = document.getElementById("doctorSideEffectPageSize");
    const resultCount = document.getElementById("doctorSideEffectResultCount");
    const currentPageLabel = document.getElementById("doctorSideEffectCurrentPage");
    const paginationButtons = Array.from(document.querySelectorAll("[data-doctor-side-effect-page]"));
    let currentPage = 1;

    function normalize(value) {
        return (value || "").toString().trim().toLowerCase();
    }

    function getFilteredCards() {
        const query = normalize(searchInput && searchInput.value);

        return cards.filter(function (card) {
            return !query || normalize(card.dataset.search).indexOf(query) !== -1;
        });
    }

    function renderDoctorSideEffects() {
        const pageSize = parseInt(pageSizeSelect && pageSizeSelect.value ? pageSizeSelect.value : "10", 10);
        const filteredCards = getFilteredCards();
        const totalPages = Math.max(1, Math.ceil(filteredCards.length / pageSize));

        if (currentPage > totalPages) {
            currentPage = totalPages;
        }

        const start = (currentPage - 1) * pageSize;
        const visibleCards = filteredCards.slice(start, start + pageSize);

        cards.forEach(function (card) {
            card.hidden = true;
        });

        visibleCards.forEach(function (card) {
            card.hidden = false;
        });

        if (emptyRow) {
            emptyRow.hidden = filteredCards.length > 0;
        }

        if (resultCount) {
            resultCount.textContent = filteredCards.length.toString();
        }

        if (currentPageLabel) {
            currentPageLabel.textContent = currentPage.toString();
        }

        paginationButtons.forEach(function (button) {
            const action = button.dataset.doctorSideEffectPage;
            button.disabled =
                (action === "first" || action === "prev") && currentPage <= 1 ||
                (action === "next" || action === "last") && currentPage >= totalPages ||
                filteredCards.length === 0;
        });
    }

    function resetAndRender() {
        currentPage = 1;
        renderDoctorSideEffects();
    }

    if (searchInput) {
        searchInput.addEventListener("input", resetAndRender);
    }

    if (pageSizeSelect) {
        pageSizeSelect.addEventListener("change", resetAndRender);
    }

    paginationButtons.forEach(function (button) {
        button.addEventListener("click", function () {
            const pageSize = parseInt(pageSizeSelect && pageSizeSelect.value ? pageSizeSelect.value : "10", 10);
            const totalPages = Math.max(1, Math.ceil(getFilteredCards().length / pageSize));

            if (button.dataset.doctorSideEffectPage === "first") {
                currentPage = 1;
            } else if (button.dataset.doctorSideEffectPage === "prev") {
                currentPage = Math.max(1, currentPage - 1);
            } else if (button.dataset.doctorSideEffectPage === "next") {
                currentPage = Math.min(totalPages, currentPage + 1);
            } else if (button.dataset.doctorSideEffectPage === "last") {
                currentPage = totalPages;
            }

            renderDoctorSideEffects();
        });
    });

    renderDoctorSideEffects();
})();
