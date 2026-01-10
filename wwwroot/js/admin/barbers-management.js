/**
 * Gestión de Barberos - JavaScript
 * Maneja todas las funcionalidades de la página de gestión de salóns
 */

(function() {
    'use strict';

    // ============================================
    // FUNCIONES DE FILTRADO Y BÚSQUEDA
    // ============================================

    function filterBarbers() {
        var search = document.getElementById("searchBarbers").value.toLowerCase();
        var statusFilter = document.getElementById("filterStatus").value;
        var rows = document.querySelectorAll("#barbersTableBody tr");
        
        for (var i = 0; i < rows.length; i++) {
            var row = rows[i];
            var name = row.dataset.name || "";
            var business = row.dataset.business || "";
            var phone = row.dataset.phone || "";
            var status = row.dataset.status || "";
            
            var matchesSearch = name.indexOf(search) !== -1 || business.indexOf(search) !== -1 || phone.indexOf(search) !== -1;
            var matchesStatus = !statusFilter || status === statusFilter;
            
            row.style.display = (matchesSearch && matchesStatus) ? "" : "none";
        }
    }

    // ============================================
    // FUNCIONES DE MODALES
    // ============================================

    function showCreateBarberModal() {
        document.getElementById("modalCrearSalón").showModal();
    }

    function closeCreateBarberModal() {
        document.getElementById("modalCrearSalón").close();
    }

    function closeEditBarberModal() {
        document.getElementById("modalEditarSalón").close();
    }

    // ============================================
    // FUNCIONES DE ACCIONES DE BARBEROS
    // ============================================

    function viewBarberDetails(id, name) {
        document.getElementById("barberDetailsTitle").textContent = "Detalles: " + name;
        document.getElementById("barberDetailsLoading").classList.remove("hidden");
        document.getElementById("barberDetailsContent").classList.add("hidden");
        document.getElementById("modalVerSalón").showModal();
        
        // Obtener dashboard del salón
        fetch("/admin/salons/" + id + "/dashboard")
        .then(function(response) { return response.ok ? response.json() : null; })
        .then(function(dashboard) {
            // Obtener resumen financiero
            return fetch("/admin/salons/" + id + "/finances/summary")
            .then(function(response) { return response.ok ? response.json() : null; })
            .then(function(finance) {
                return { dashboard: dashboard, finance: finance };
            });
        })
        .then(function(data) {
            // Obtener servicios
            return fetch("/admin/salons/" + id + "/services")
            .then(function(response) { return response.ok ? response.json() : []; })
            .then(function(services) {
                data.services = services;
                return data;
            });
        })
        .then(function(data) {
            // Obtener citas
            return fetch("/admin/salons/" + id + "/appointments")
            .then(function(response) { return response.ok ? response.json() : []; })
            .then(function(appointments) {
                data.appointments = appointments;
                return data;
            });
        })
        .then(function(data) {
            var dashboard = data.dashboard;
            var finance = data.finance;
            var services = data.services;
            var appointments = data.appointments;
            
            // Llenar información del salón
            if (dashboard && dashboard.barber) {
                document.getElementById("detailBarberName").textContent = dashboard.barber.name || "-";
                document.getElementById("detailBarberBusiness").textContent = dashboard.barber.businessName || "-";
                document.getElementById("detailBarberPhone").textContent = dashboard.barber.phone || "-";
                document.getElementById("detailBarberSlug").textContent = dashboard.barber.slug || "-";
                if (dashboard.barber.email) {
                    document.getElementById("detailBarberEmail").textContent = dashboard.barber.email;
                }
                document.getElementById("detailBarberStatus").innerHTML = dashboard.barber.isActive 
                    ? '<span class="badge badge-success badge-xs">Activo</span>' 
                    : '<span class="badge badge-error badge-xs">Inactivo</span>';
                if (dashboard.barber.createdAt) {
                    var date = new Date(dashboard.barber.createdAt);
                    document.getElementById("detailBarberCreated").textContent = date.toLocaleDateString("es-ES");
                }
            }
            
            // Llenar estadísticas del día
            if (dashboard && dashboard.today) {
                document.getElementById("detailTodayAppointments").textContent = dashboard.today.appointments || 0;
                document.getElementById("detailTodayCompleted").textContent = dashboard.today.completed || 0;
                document.getElementById("detailTodayPending").textContent = dashboard.today.pending || 0;
                document.getElementById("detailTodayIncome").textContent = "$" + (dashboard.today.income || 0).toFixed(2);
            }
            
            // Llenar estadísticas de la semana
            if (dashboard && dashboard.thisWeek) {
                document.getElementById("detailWeekAppointments").textContent = dashboard.thisWeek.appointments || 0;
                document.getElementById("detailWeekIncome").textContent = "$" + (dashboard.thisWeek.income || 0).toFixed(2);
                document.getElementById("detailWeekExpenses").textContent = "$" + (dashboard.thisWeek.expenses || 0).toFixed(2);
                document.getElementById("detailWeekProfit").textContent = "$" + (dashboard.thisWeek.profit || 0).toFixed(2);
            }
            
            // Llenar estadísticas del mes
            if (dashboard && dashboard.thisMonth) {
                document.getElementById("detailMonthAppointments").textContent = dashboard.thisMonth.appointments || 0;
                document.getElementById("detailMonthIncome").textContent = "$" + (dashboard.thisMonth.income || 0).toFixed(2);
                document.getElementById("detailMonthExpenses").textContent = "$" + (dashboard.thisMonth.expenses || 0).toFixed(2);
                document.getElementById("detailMonthProfit").textContent = "$" + (dashboard.thisMonth.profit || 0).toFixed(2);
            }
            
            // Llenar resumen financiero total
            if (finance) {
                document.getElementById("detailTotalIncome").textContent = "$" + (finance.totalIncome || 0).toFixed(2);
                document.getElementById("detailTotalExpenses").textContent = "$" + (finance.totalExpenses || 0).toFixed(2);
                document.getElementById("detailNetProfit").textContent = "$" + (finance.netProfit || 0).toFixed(2);
            }
            
            // Llenar servicios
            var servicesHtml = services.length > 0 
                ? services.map(function(s) {
                    return '<div class="flex justify-between items-center p-3 bg-base-100 rounded-lg border border-base-300 mb-2">' +
                        '<div><span class="font-medium text-sm">' + s.name + '</span>' +
                        '<span class="text-xs text-gray-500 ml-2">' + s.durationMinutes + ' min</span></div>' +
                        '<span class="text-sm font-bold text-gold">$' + s.price.toFixed(2) + '</span>' +
                        '</div>';
                }).join("")
                : '<p class="text-sm text-gray-500 text-center py-4">No hay servicios registrados</p>';
            document.getElementById("detailServices").innerHTML = servicesHtml;
            
            // Función para formatear citas
            var formatAppointment = function(a) {
                var statusClass = (a.status === "Completed" || a.status === "Confirmed") ? "badge-success" : 
                                 (a.status === "Pending") ? "badge-warning" : "badge-info";
                return '<div class="p-3 bg-base-100 rounded-lg border border-base-300 mb-2">' +
                    '<div class="flex justify-between items-start mb-1">' +
                    '<div class="flex-1">' +
                    '<div class="font-medium text-sm">' + (a.clientName || "Sin nombre") + '</div>' +
                    '<div class="text-xs text-gray-500 mt-1">' + (a.serviceName || "Sin servicio") + '</div>' +
                    '</div>' +
                    '<span class="badge badge-sm ' + statusClass + '">' + (a.status || "N/A") + '</span>' +
                    '</div>' +
                    '<div class="text-xs text-gray-400 mt-2">' + (a.date || "") + " " + (a.time || "") + '</div>' +
                    '</div>';
            };
            
            // Llenar citas recientes
            if (dashboard && dashboard.recentAppointments && dashboard.recentAppointments.length > 0) {
                var recentHtml = dashboard.recentAppointments.map(formatAppointment).join("");
                document.getElementById("detailRecentAppointments").innerHTML = recentHtml;
            } else if (appointments && appointments.length > 0) {
                var today = new Date();
                today.setHours(0, 0, 0, 0);
                var recentAppts = appointments.filter(function(a) {
                    var appDate = new Date(a.date);
                    return appDate < today;
                }).slice(0, 5);
                
                var recentHtml = recentAppts.length > 0
                    ? recentAppts.map(formatAppointment).join("")
                    : '<p class="text-sm text-gray-500 text-center py-4">No hay citas recientes</p>';
                document.getElementById("detailRecentAppointments").innerHTML = recentHtml;
            } else {
                document.getElementById("detailRecentAppointments").innerHTML = '<p class="text-sm text-gray-500 text-center py-4">No hay citas recientes</p>';
            }
            
            // Llenar próximas citas
            if (dashboard && dashboard.upcomingAppointments && dashboard.upcomingAppointments.length > 0) {
                var upcomingHtml = dashboard.upcomingAppointments.map(formatAppointment).join("");
                document.getElementById("detailUpcomingAppointments").innerHTML = upcomingHtml;
            } else if (appointments && appointments.length > 0) {
                var today = new Date();
                today.setHours(0, 0, 0, 0);
                var upcomingAppts = appointments.filter(function(a) {
                    var appDate = new Date(a.date);
                    return appDate >= today;
                }).slice(0, 5);
                
                var upcomingHtml = upcomingAppts.length > 0
                    ? upcomingAppts.map(formatAppointment).join("")
                    : '<p class="text-sm text-gray-500 text-center py-4">No hay próximas citas</p>';
                document.getElementById("detailUpcomingAppointments").innerHTML = upcomingHtml;
            } else {
                document.getElementById("detailUpcomingAppointments").innerHTML = '<p class="text-sm text-gray-500 text-center py-4">No hay próximas citas</p>';
            }
            
            // Mostrar contenido
            document.getElementById("barberDetailsLoading").classList.add("hidden");
            document.getElementById("barberDetailsContent").classList.remove("hidden");
        })
        .catch(function(error) {
            console.error("Error al cargar detalles:", error);
            document.getElementById("barberDetailsLoading").innerHTML = 
                '<p class="text-error">Error al cargar la información del salón: ' + error.message + '</p>';
        });
    }

    function editBarber(id, name, business, phone) {
        document.getElementById("editBarberId").value = id;
        document.getElementById("editBarberName").value = name || "";
        document.getElementById("editBarberBusiness").value = business || "";
        document.getElementById("editBarberPhone").value = phone || "";
        // Limpiar campo de contraseña al abrir el modal
        document.getElementById("editPasswordInput").value = "";
        document.getElementById("copyEditPasswordBtn").style.display = "none";
        document.getElementById("modalEditarSalón").showModal();
    }

    function generateEditPassword() {
        var length = 12;
        var lower = "abcdefghijklmnopqrstuvwxyz";
        var upper = "ABCDEFGHIJKLMNOPQRSTUVWXYZ";
        var numbers = "0123456789";
        var charAt = String.fromCharCode(64);
        var charHash = String.fromCharCode(35);
        var special = "!" + charAt + charHash + "$%&*";
        var charset = lower + upper + numbers + special;
        var password = "";
        
        password += upper[Math.floor(Math.random() * 26)];
        password += lower[Math.floor(Math.random() * 26)];
        password += numbers[Math.floor(Math.random() * 10)];
        var specialChars = ["!", charAt, charHash, "$", "%", "&", "*"];
        password += specialChars[Math.floor(Math.random() * specialChars.length)];
        
        for (var i = password.length; i < length; i++) {
            password += charset[Math.floor(Math.random() * charset.length)];
        }
        
        password = password.split("").sort(function() { return Math.random() - 0.5; }).join("");
        
        var passwordInput = document.getElementById("editPasswordInput");
        var copyBtn = document.getElementById("copyEditPasswordBtn");
        if (passwordInput) {
            passwordInput.type = "text";
            passwordInput.value = password;
            passwordInput.select();
            
            if (copyBtn) {
                copyBtn.style.display = "block";
            }
            
            setTimeout(function() {
                passwordInput.type = "password";
            }, 5000);
        }
    }

    function copyEditPassword() {
        var passwordInput = document.getElementById("editPasswordInput");
        if (!passwordInput || !passwordInput.value) {
            alert("No hay contraseña para copiar");
            return;
        }
        
        var wasPassword = passwordInput.type === "password";
        if (wasPassword) {
            passwordInput.type = "text";
        }
        
        passwordInput.select();
        passwordInput.setSelectionRange(0, 99999);
        
        try {
            document.execCommand("copy");
            alert("Contraseña copiada al portapapeles");
        } catch (err) {
            if (navigator.clipboard && navigator.clipboard.writeText) {
                navigator.clipboard.writeText(passwordInput.value).then(function() {
                    alert("Contraseña copiada al portapapeles");
                }).catch(function() {
                    alert("Error al copiar. Por favor selecciona y copia manualmente.");
                });
            } else {
                alert("Error al copiar. Por favor selecciona y copia manualmente.");
            }
        }
        
        if (wasPassword) {
            passwordInput.type = "password";
        }
    }

    function toggleBarberStatus(id, isActive) {
        var message = "¿Estás seguro de " + (isActive ? "desactivar" : "activar") + " este salón?";
        if (!confirm(message)) {
            return;
        }

        fetch("/admin/salons/" + id + "/status", {
            method: "POST",
            headers: { "Content-Type": "application/json" },
            body: JSON.stringify({ isActive: !isActive })
        })
        .then(function(r) { return r.json(); })
        .then(function(data) {
            if (data.success) {
                location.reload();
            } else {
                alert(data.message || "Error al actualizar estado");
            }
        })
        .catch(function(err) {
            console.error(err);
            alert("Error al actualizar estado");
        });
    }

    function confirmDeleteBarber(id, name) {
        document.getElementById("barberNameToDelete").textContent = name || "este salón";
        document.getElementById("modalEliminarSalón").setAttribute("data-barber-id", id);
        document.getElementById("modalEliminarSalón").showModal();
    }

    function deleteBarber() {
        var modal = document.getElementById("modalEliminarSalón");
        var id = modal.getAttribute("data-barber-id");
        
        if (!id) {
            alert("Error: No se pudo identificar el salón a eliminar");
            return;
        }

        fetch("/admin/salons/" + id, {
            method: "DELETE"
        })
        .then(function(response) {
            if (response.status === 204 || response.ok) {
                return { success: true, message: "Salón eliminado exitosamente" };
            }
            if (response.status === 404) {
                return fetch("/admin/salons/" + id + "/delete", {
                    method: "POST"
                }).then(function(r) {
                    if (r.ok || r.status === 204) {
                        return { success: true, message: "Salón eliminado exitosamente" };
                    }
                    return r.json();
                });
            }
            return response.json();
        })
        .then(function(result) {
            if (result.success) {
                alert("Salón eliminado exitosamente");
                modal.close();
                location.reload();
            } else {
                alert("Error: " + (result.message || "No se pudo eliminar el salón"));
            }
        })
        .catch(function(error) {
            console.error("Error:", error);
            alert("Error al eliminar salón: " + error.message);
        });
    }

    // ============================================
    // FUNCIONES DE CONTRASEÑA
    // ============================================

    function generatePassword() {
        var length = 12;
        var lower = "abcdefghijklmnopqrstuvwxyz";
        var upper = "ABCDEFGHIJKLMNOPQRSTUVWXYZ";
        var numbers = "0123456789";
        var charAt = String.fromCharCode(64);
        var charHash = String.fromCharCode(35);
        var special = "!" + charAt + charHash + "$%&*";
        var charset = lower + upper + numbers + special;
        var password = "";
        
        password += upper[Math.floor(Math.random() * 26)];
        password += lower[Math.floor(Math.random() * 26)];
        password += numbers[Math.floor(Math.random() * 10)];
        var specialChars = ["!", charAt, charHash, "$", "%", "&", "*"];
        password += specialChars[Math.floor(Math.random() * specialChars.length)];
        
        for (var i = password.length; i < length; i++) {
            password += charset[Math.floor(Math.random() * charset.length)];
        }
        
        password = password.split("").sort(function() { return Math.random() - 0.5; }).join("");
        
        var passwordInput = document.getElementById("passwordInput");
        var copyBtn = document.getElementById("copyPasswordBtn");
        if (passwordInput) {
            passwordInput.type = "text";
            passwordInput.value = password;
            passwordInput.select();
            
            if (copyBtn) {
                copyBtn.style.display = "block";
            }
            
            setTimeout(function() {
                passwordInput.type = "password";
            }, 5000);
        }
    }

    function copyPassword() {
        var passwordInput = document.getElementById("passwordInput");
        if (!passwordInput || !passwordInput.value) {
            alert("No hay contraseña para copiar");
            return;
        }
        
        var wasPassword = passwordInput.type === "password";
        if (wasPassword) {
            passwordInput.type = "text";
        }
        
        passwordInput.select();
        passwordInput.setSelectionRange(0, 99999);
        
        try {
            document.execCommand("copy");
            alert("Contraseña copiada al portapapeles");
        } catch (err) {
            if (navigator.clipboard && navigator.clipboard.writeText) {
                navigator.clipboard.writeText(passwordInput.value).then(function() {
                    alert("Contraseña copiada al portapapeles");
                }).catch(function() {
                    alert("Error al copiar. Por favor selecciona y copia manualmente.");
                });
            } else {
                alert("Error al copiar. Por favor selecciona y copia manualmente.");
            }
        }
        
        if (wasPassword) {
            passwordInput.type = "password";
        }
    }

    // ============================================
    // FUNCIONES DE EXPORTACIÓN
    // ============================================

    function exportToExcel() {
        var table = document.getElementById("barbersTable");
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
        link.download = "salóns_" + dateStr + ".csv";
        link.click();
    }

    function exportToPDF() {
        var table = document.getElementById("barbersTable");
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
        printWindow.document.write("<title>Reporte de Barberos</title>");
        printWindow.document.write("<style>");
        printWindow.document.write("body{font-family:Arial,sans-serif;padding:20px;}");
        printWindow.document.write("table{width:100%;border-collapse:collapse;margin-top:20px;}");
        printWindow.document.write("th,td{border:1px solid #ddd;padding:8px;text-align:left;}");
        printWindow.document.write("th{background-color:#f2f2f2;font-weight:bold;}");
        printWindow.document.write("h1{color:#333;}");
        printWindow.document.write("</style>");
        printWindow.document.write("</head>");
        printWindow.document.write("<body>");
        printWindow.document.write("<h1>Reporte de Barberos - " + dateStr + "</h1>");
        printWindow.document.write(tableHtml);
        printWindow.document.write("</body>");
        printWindow.document.write("</html>");
        printWindow.document.close();
        printWindow.print();
    }

    // ============================================
    // MANEJO DE FORMULARIOS
    // ============================================

    function handleCreateBarberForm(e) {
        e.preventDefault();
        var formData = new FormData(e.target);
        
        var email = formData.get("email");
        email = email ? email.toString().trim() : "";
        var password = formData.get("password");
        password = password ? password.toString().trim() : "";
        var name = formData.get("name");
        name = name ? name.toString().trim() : "";
        var businessName = formData.get("businessName");
        businessName = businessName ? businessName.toString().trim() : null;
        var phone = formData.get("phone");
        phone = phone ? phone.toString().trim() : "";
        
        if (!email || !password || !name || !phone) {
            alert("Por favor completa todos los campos requeridos");
            return;
        }
        
        var requestData = {
            email: email,
            password: password,
            name: name,
            businessName: businessName || null,
            phone: phone
        };
        
        console.log("Enviando datos:", requestData);
        
        var tokenInput = document.querySelector('input[name="__RequestVerificationToken"]');
        var token = tokenInput ? tokenInput.value : "";
        
        fetch("/admin/createsalon", {
            method: "POST",
            headers: {
                "Content-Type": "application/json",
                "RequestVerificationToken": token
            },
            body: JSON.stringify(requestData)
        })
        .then(function(response) { return response.json(); })
        .then(function(result) {
            console.log("Respuesta del servidor:", result);
            if (result.success) {
                var barberName = result.barber && result.barber.name ? result.barber.name : "";
                alert("Salón creado exitosamente: " + barberName);
                document.getElementById("modalCrearSalón").close();
                e.target.reset();
                setTimeout(function() {
                    location.reload();
                }, 500);
            } else {
                alert("Error: " + (result.message || "No se pudo crear el salón"));
            }
        })
        .catch(function(error) {
            console.error("Error:", error);
            alert("Error al crear salón: " + error.message);
        });
    }

    function handleEditBarberForm(e) {
        e.preventDefault();
        var formData = new FormData(e.target);
        
        var id = formData.get("id");
        var name = formData.get("name");
        name = name ? name.toString().trim() : "";
        var businessName = formData.get("businessName");
        businessName = businessName ? businessName.toString().trim() : null;
        var phone = formData.get("phone");
        phone = phone ? phone.toString().trim() : "";
        var password = formData.get("password");
        password = password ? password.toString().trim() : null;
        
        if (!id || !name || !phone) {
            alert("Por favor completa todos los campos requeridos");
            return;
        }
        
        var requestData = {
            name: name,
            businessName: businessName || null,
            phone: phone
        };
        
        // Agregar contraseña solo si se proporciona
        if (password && password.length > 0) {
            if (password.length < 6) {
                alert("La contraseña debe tener al menos 6 caracteres");
                return;
            }
            requestData.password = password;
        }
        
        var tokenInput = document.querySelector('input[name="__RequestVerificationToken"]');
        var token = tokenInput ? tokenInput.value : "";
        
        fetch("/admin/salons/" + id, {
            method: "PUT",
            headers: {
                "Content-Type": "application/json",
                "RequestVerificationToken": token
            },
            body: JSON.stringify(requestData)
        })
        .then(function(response) { return response.json(); })
        .then(function(result) {
            if (result.success) {
                alert("Salón actualizado exitosamente");
                document.getElementById("modalEditarSalón").close();
                setTimeout(function() {
                    location.reload();
                }, 500);
            } else {
                alert("Error: " + (result.message || "No se pudo actualizar el salón"));
            }
        })
        .catch(function(error) {
            console.error("Error:", error);
            alert("Error al actualizar salón: " + error.message);
        });
    }

    // ============================================
    // INICIALIZACIÓN
    // ============================================

    document.addEventListener("DOMContentLoaded", function() {
        // Botón crear salón
        var btnCrear = document.getElementById("btnCrearSalón");
        if (btnCrear) {
            btnCrear.addEventListener("click", showCreateBarberModal);
        }

        // Botones cerrar modales
        var btnCloseCreate = document.getElementById("btnCloseCreateModal");
        if (btnCloseCreate) {
            btnCloseCreate.addEventListener("click", closeCreateBarberModal);
        }

        var btnCloseEdit = document.getElementById("btnCloseEditModal");
        if (btnCloseEdit) {
            btnCloseEdit.addEventListener("click", closeEditBarberModal);
        }

        var btnCloseView = document.getElementById("btnCloseViewModal");
        if (btnCloseView) {
            btnCloseView.addEventListener("click", function() {
                document.getElementById("modalVerSalón").close();
            });
        }

        // Inputs de búsqueda y filtro
        var searchInput = document.getElementById("searchBarbers");
        if (searchInput) {
            searchInput.addEventListener("keyup", filterBarbers);
        }

        var filterStatus = document.getElementById("filterStatus");
        if (filterStatus) {
            filterStatus.addEventListener("change", filterBarbers);
        }

        // Botones de acciones en la tabla
        var actionButtons = document.querySelectorAll("[data-action]");
        for (var i = 0; i < actionButtons.length; i++) {
            (function(btn) {
                var action = btn.getAttribute("data-action");
                
                if (action === "view") {
                    btn.addEventListener("click", function() {
                        var id = parseInt(btn.getAttribute("data-id"));
                        var name = btn.getAttribute("data-name") || "";
                        viewBarberDetails(id, name);
                    });
                } else if (action === "edit") {
                    btn.addEventListener("click", function() {
                        var id = parseInt(btn.getAttribute("data-id"));
                        var name = btn.getAttribute("data-name") || "";
                        var business = btn.getAttribute("data-business") || "";
                        var phone = btn.getAttribute("data-phone") || "";
                        editBarber(id, name, business, phone);
                    });
                } else if (action === "toggle") {
                    btn.addEventListener("click", function() {
                        var id = parseInt(btn.getAttribute("data-id"));
                        var isActive = btn.getAttribute("data-active") === "true";
                        toggleBarberStatus(id, isActive);
                    });
                } else if (action === "delete") {
                    btn.addEventListener("click", function() {
                        var id = parseInt(btn.getAttribute("data-id"));
                        var name = btn.getAttribute("data-name") || "";
                        confirmDeleteBarber(id, name);
                    });
                }
            })(actionButtons[i]);
        }

        // Botones del modal de eliminar
        var btnConfirmDelete = document.getElementById("btnConfirmDelete");
        if (btnConfirmDelete) {
            btnConfirmDelete.addEventListener("click", deleteBarber);
        }

        var btnCancelDelete = document.getElementById("btnCancelDelete");
        if (btnCancelDelete) {
            btnCancelDelete.addEventListener("click", function() {
                document.getElementById("modalEliminarSalón").close();
            });
        }

        // Formularios
        var formCrear = document.getElementById("formCrearSalón");
        if (formCrear) {
            formCrear.addEventListener("submit", handleCreateBarberForm);
        }

        var formEditar = document.getElementById("formEditarSalón");
        if (formEditar) {
            formEditar.addEventListener("submit", handleEditBarberForm);
        }

        // Botones de generar y copiar contraseña (modal crear)
        var btnGeneratePassword = document.getElementById("btnGeneratePassword");
        if (btnGeneratePassword) {
            btnGeneratePassword.addEventListener("click", generatePassword);
        }

        var btnCopyPassword = document.getElementById("copyPasswordBtn");
        if (btnCopyPassword) {
            btnCopyPassword.addEventListener("click", copyPassword);
        }

        // Botones de generar y copiar contraseña (modal editar)
        var btnGenerateEditPassword = document.getElementById("btnGenerateEditPassword");
        if (btnGenerateEditPassword) {
            btnGenerateEditPassword.addEventListener("click", generateEditPassword);
        }

        var btnCopyEditPassword = document.getElementById("copyEditPasswordBtn");
        if (btnCopyEditPassword) {
            btnCopyEditPassword.addEventListener("click", copyEditPassword);
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
    });

})();

