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


folder <- "cassandra/debug/"
f1 <- paste0(folder,"jfunc_debug_treat_ranking_period_16.xlsx")
d1 <- read_xlsx(f1)

tt <- d1 %>%  filter(elem_index == 45)

f2 <- paste0(folder,"debug_treat_ranking_period_16.xlsx")
d2 <- read_xlsx(f2)

# cc <- data.frame(jfunc_cost = d1$treatment_cost, jfunc_picked = d1$selected,
#                  c_sharp_cost = d2$treatment_cost, c_sharp_picked = d2$selected)
# cc$diff_cost <- cc$jfunc_cost - cc$c_sharp_cost
# cc$diff_sel <- cc$jfunc_picked - cc$c_sharp_picked


tmp <- rbind(d1,d2)
indexes <- tmp %>% arrange(elem_index)
indexes <- unique(indexes$elem_index)
n <- length(indexes)

result <- data.frame(elem_index = indexes, 
                     jfunc = rep(NA,n), c_sharp = rep(NA,n))

# Count number of treatments on an element
get_count <- function(index, df) {
  n <- 0
  for (i in 1:nrow(df)) {
    idx <- df[[i,"elem_index"]]
    if (idx == index) {
      n <- n + 1
    }
  }
  return(n)
}


# get list of Jfunc vs C# treatment counts for each index
for (i in 1:nrow(result)) {
    index <- result[[i, "elem_index"]]
    count1 <- get_count(index, d1)
    count2 <- get_count(index, d2)
    result[i, "jfunc"] <- count1
    result[i, "c_sharp"] <- count2
}

result$has_discrep <- ifelse(result$jfunc != result$c_sharp,TRUE, FALSE)

discreps <- result %>% filter(has_discrep == TRUE)

t1 <- d1 %>% filter(elem_index == 1240)
t2 <- d2 %>% filter(elem_index == 1240)
tt <- rbind(t1,t2)
