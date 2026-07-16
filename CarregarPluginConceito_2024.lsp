;;; Carrega automaticamente o plugin .NET do CNT Cad no ZWCAD 2024.
;;; Coloque este arquivo no Startup Suite do ZWCAD 2024.

(defun CNT:LoadPluginConceito2024 (/ dll oldFileDia oldCmdDia result)
  (setq dll "Z:/Transfer/Z.Implementação BIM e Processos/7. Repo/lisp/plugin/2024/PluginConceito.dll")

  (if (findfile dll)
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
            "\n[CNT] Falha ao carregar PluginConceito.dll: "
            (vl-catch-all-error-message result)
          )
        )
        (princ "\n[CNT] PluginConceito.dll carregado.")
      )
    )
    (princ
      (strcat
        "\n[CNT] PluginConceito.dll nao encontrado em: "
        dll
      )
    )
  )

  (princ)
)

(CNT:LoadPluginConceito2024)
(princ)
