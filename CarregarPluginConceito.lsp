;;; CNT Cad - carregador local ZWCAD 2025
;;; Use este LISP no Startup Suite do ZWCAD 2025 durante desenvolvimento.

(vl-load-com)

(defun CNT:LoadPluginConceito (/ dll oldFileDia oldCmdDia result)
  (setq dll "C:/Users/LuizOtavio/source/repos/CTNCad/PluginConceito/bin/Debug/PluginConceito.dll")

  (if (not (findfile dll))
    (princ
      (strcat
        "\n[CNT] PluginConceito.dll 2025 nao encontrado em: "
        dll
      )
    )
    (progn
      (setq oldFileDia (getvar "FILEDIA"))
      (setq oldCmdDia (getvar "CMDDIA"))

      (setvar "FILEDIA" 0)
      (setvar "CMDDIA" 0)

      (setq result
        (vl-catch-all-apply
          'vl-cmdf
          (list "_.NETLOAD" dll)
        )
      )

      (setvar "FILEDIA" oldFileDia)
      (setvar "CMDDIA" oldCmdDia)

      (if (vl-catch-all-error-p result)
        (princ
          (strcat
            "\n[CNT] Falha ao solicitar NETLOAD do PluginConceito.dll 2025: "
            (vl-catch-all-error-message result)
          )
        )
        (princ
          (strcat
            "\n[CNT] NETLOAD solicitado para PluginConceito.dll 2025: "
            dll
          )
        )
      )
    )
  )

  (princ)
)

(CNT:LoadPluginConceito)
(princ)
