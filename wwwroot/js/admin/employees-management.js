/**
 * Gestión de Empleados - JavaScript
 * Maneja todas las funcionalidades de la página de gestión de empleados
 */

(function() {
    'use strict';

    // ============================================
    // FUNCIONES DE FILTRADO Y BÚSQUEDA
    // ============================================

    function filterEmployees() {
        var search = document.getElementById("searchEmployees").value.toLowerCase();
        var barberFilter = document.getElementById("filterBarber").value;
        var statusFilter = document.getElementById("filterStatus").value;
        var rows = document.querySelectorAll("#employeesTableBody tr");
        
        for (var i = 0; i < rows.length; i++) {
            var row = rows[i];
            var name = row.dataset.name || "";
            var email = row.dataset.email || "";
            var barberId = row.dataset.barberId || "";
            var status = row.dataset.status || "";
            
            var matchesSearch = name.indexOf(search) !== -1 || email.indexOf(search) !== -1;
            var matchesBarber = !barberFilter || barberId === barberFilter;
            var matchesStatus = !statusFilter || status === statusFilter;
            
            row.style.display = (matchesSearch && matchesBarber && matchesStatus) ? "" : "none";
        }
    }

    // ============================================
    // FUNCIONES DE ACCIONES DE EMPLEADOS
    // ============================================

    function viewEmployeeDetails(employeeId, barberId) {
        window.location.href = "/admin/dashboard#barber-" + barberId;
    }

    // ============================================
    // FUNCIONES DE EXPORTACIÓN
    // ============================================

    function exportToExcel() {
        var table = document.getElementById("employeesTable");
        if (!table) return;

        var csv = "";
        var rows = table.querySelectorAll("tr");

        for (var i = 0; i < rows.length; i++) {
            var row = rows[i];
            var cols = row.querySelectorAll("th, td");
            var rowData = [];
            for (var j = 0; j < cols.length; j++) {
                var text = cols[j].innerText.trim();
                if (text.indexOf(",") !== -1 || text.indexOf('"') !== -1) {
                    text = '"' + text.replace(/"/g, '""') + '"';
                }
                rowData.push(text);
            }
            csv += rowData.join(",") + "\n";
        }

        var blob = new Blob([csv], { type: "text/csv;charset=utf-8;" });
        var link = document.createElement("a");
        link.href = URL.createObjectURL(blob);
        var dateStr = new Date().toISOString().split("T")[0];
        link.download = "empleados_" + dateStr + ".csv";
        link.click();
    }

    function exportToPDF() {
        var table = document.getElementById("employeesTable");
        if (!table) return;
        var printWindow = window.open("", "_blank");
        if (!printWindow) {
            alert("Por favor permite ventanas emergentes para exportar a PDF");
            return;
        }
        var dateStr = new Date().toLocaleDateString();
        var tableHtml = table.outerHTML;
        printWindow.document.open();
        printWindow.document.write("<!DOCTYPE html>");
        printWindow.document.write("<html>");
        printWindow.document.write("<head>");
        printWindow.document.write("<title>Reporte de Empleados</title>");
        printWindow.document.write("<style>");
        printWindow.document.write("body{font-family:Arial,sans-serif;padding:20px;}");
        printWindow.document.write("table{width:100%;border-collapse:collapse;margin-top:20px;}");
        printWindow.document.write("th,td{border:1px solid #ddd;padding:8px;text-align:left;}");
        printWindow.document.write("th{background-color:#f2f2f2;font-weight:bold;}");
        printWindow.document.write("h1{color:#333;}");
        printWindow.document.write("</style>");
        printWindow.document.write("</head>");
        printWindow.document.write("<body>");
        printWindow.document.write("<h1>Reporte de Empleados - " + dateStr + "</h1>");
        printWindow.document.write(tableHtml);
        printWindow.document.write("</body>");
        printWindow.document.write("</html>");
        printWindow.document.close();
        printWindow.print();
    }

    // ============================================
    // INICIALIZACIÓN
    // ============================================

    document.addEventListener("DOMContentLoaded", function() {
        // Inputs de búsqueda y filtro
        var searchInput = document.getElementById("searchEmployees");
        if (searchInput) {
            searchInput.addEventListener("keyup", filterEmployees);
        }

        var filterBarber = document.getElementById("filterBarber");
        if (filterBarber) {
            filterBarber.addEventListener("change", filterEmployees);
        }

        var filterStatus = document.getElementById("filterStatus");
        if (filterStatus) {
            filterStatus.addEventListener("change", filterEmployees);
        }

        // Botones de exportación
        var btnExportExcel = document.getElementById("btnExportExcel");
        if (btnExportExcel) {
            btnExportExcel.addEventListener("click", exportToExcel);
        }

        var btnExportPDF = document.getElementById("btnExportPDF");
        if (btnExportPDF) {
            btnExportPDF.addEventListener("click", exportToPDF);
        }

        // Botones de ver detalles
        var viewButtons = document.querySelectorAll("[data-action='view']");
        for (var i = 0; i < viewButtons.length; i++) {
            (function(btn) {
                btn.addEventListener("click", function() {
                    var employeeId = parseInt(btn.getAttribute("data-employee-id"));
                    var barberId = parseInt(btn.getAttribute("data-barber-id"));
                    viewEmployeeDetails(employeeId, barberId);
                });
            })(viewButtons[i]);
        }
    });

})();

