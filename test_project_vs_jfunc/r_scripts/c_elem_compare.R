#-------------------------------------------------------------------------------
#
#  Compares the indexes of elements with triggered treatments in Period 1
#
#-------------------------------------------------------------------------------
rm(list = ls())
library(tidyverse)
library(readxl)
library(clipr)
library(DescTools)

source("r_scripts/z_support1.R")

folder <- "cassandra/outputs/"

params <- get_params()

get_elem_data <- function(ielem, year) {
  result <- NULL
  col <- paste0("X", year)
  for (param in params) {
    
    f1 <- paste0(folder, param, "_current_jfunc.csv")
    d1 <- read.csv(f1)
    
    f2 <- paste0(folder, param, "_current.csv")
    d2 <- read.csv(f2)
    
    c1 <- (d1 %>% filter(elem_index == ielem) %>% select(col))[[col]]
    c2 <- (d2 %>% filter(elem_index == ielem) %>% select(col))[[col]]
  
    if (is.null((result))) {
      result <- data.frame(parameter = c(param), jfunc = c(c1), c_sharp = c(c2))
    } else {
      aa <- data.frame(parameter = c(param), jfunc = c(c1), c_sharp = c(c2))
      result <- rbind(result, aa)
    }
  }
  result[, "diff"] <- ifelse(result[,"jfunc"] == result[, "c_sharp"],0,1)
  return(result)
}

tt <- get_elem_data(1998, 2023)
