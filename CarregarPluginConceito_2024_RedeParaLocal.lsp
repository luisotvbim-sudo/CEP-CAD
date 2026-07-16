;;; CNT Cad - carregador ZWCAD 2024
;;; Copia o plugin da rede para uma pasta local do usuario antes do NETLOAD.
;;; Isso evita falha de load por caminho de rede e evita bloquear a DLL central.

(vl-load-com)

(defun CNT:EnsureDir (dir)
  (if (and dir (not (vl-file-directory-p dir)))
    (vl-mkdir dir)
  )
)

(defun CNT:LoadPluginConceito2024 (/ sourceDll sourcePdb localRoot localDir stamp localDll localPdb oldFileDia oldCmdDia copyResult loadResult)
  (setq sourceDll "Z:/Transfer/CNT_Plugins/PluginConceito/2024/PluginConceito.dll")
  (setq sourcePdb "Z:/Transfer/CNT_Plugins/PluginConceito/2024/PluginConceito.pdb")

  (if (not (findfile sourceDll))
    (progn
      (princ (strcat "\n[CNT] PluginConceito.dll nao encontrado na rede: " sourceDll))
      (princ)
    )
    (progn
      (setq localRoot (getenv "LOCALAPPDATA"))
      (if (not localRoot) (setq localRoot (getenv "TEMP")))
      (if (not localRoot) (setq localRoot "."))

      (setq localRoot (strcat localRoot "\\CNT"))
      (CNT:EnsureDir localRoot)

      (setq localRoot (strcat localRoot "\\PluginConceito"))
      (CNT:EnsureDir localRoot)

      (setq localDir (strcat localRoot "\\2024"))
      (CNT:EnsureDir localDir)

      ;; Nome unico por carregamento para nao tentar sobrescrever DLL local travada.
      (setq stamp (vl-string-translate "." "_" (rtos (getvar "CDATE") 2 8)))
      (setq localDll (strcat localDir "\\PluginConceito_" stamp ".dll"))
      (setq localPdb (strcat localDir "\\PluginConceito_" stamp ".pdb"))

      (setq copyResult (vl-catch-all-apply 'vl-file-copy (list sourceDll localDll)))

      (if (vl-catch-all-error-p copyResult)
        (princ
          (strcat
            "\n[CNT] Falha ao copiar PluginConceito.dll para pasta local: "
            (vl-catch-all-error-message copyResult)
          )
        )
        (progn
          (if (findfile sourcePdb)
            (vl-catch-all-apply 'vl-file-copy (list sourcePdb localPdb))
          )

          (setq oldFileDia (getvar "FILEDIA"))
          (setq oldCmdDia (getvar "CMDDIA"))

          (setvar "FILEDIA" 0)
          (setvar "CMDDIA" 0)

          (setq loadResult
            (vl-catch-all-apply
              'vl-cmdf
              (list "_.NETLOAD" localDll)
            )
          )

          (setvar "FILEDIA" oldFileDia)
          (setvar "CMDDIA" oldCmdDia)

          (if (vl-catch-all-error-p loadResult)
            (princ
              (strcat
                "\n[CNT] Falha ao carregar PluginConceito.dll local: "
                (vl-catch-all-error-message loadResult)
                "\n[CNT] DLL local: "
                localDll
              )
            )
            (princ
              (strcat
                "\n[CNT] PluginConceito.dll carregado da copia local: "
                localDll
              )
            )
          )
        )
      )
    )
  )

  (princ)
)

(CNT:LoadPluginConceito2024)
(princ)
